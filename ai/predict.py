import sys
import json
from pathlib import Path

import joblib
import numpy as np
import pandas as pd
from catboost import CatBoostRegressor
from xgboost import XGBRegressor


# ============================================================
# CONFIG
# ============================================================

BASE_DIR = Path(__file__).resolve().parent
MODEL_DIR = BASE_DIR / "models"

MAX_HOURS = 90 * 24
FINAL_TEST_START = pd.Timestamp("2026-08-04")

CAT_FEATURES = [
    "Tên thủ tục hành chính",
    "Tên lĩnh vực",
    "Phòng ban",
    "Cán bộ xử lý hiện tại",
]

NUM_FEATURES = [
    "sla_hours",
    "sla_log",

    "accept_hour",
    "accept_dayofweek",
    "accept_day",
    "accept_month",
    "accept_weekofyear",
    "is_weekend",

    "dow_sin",
    "dow_cos",
    "hour_sin",
    "hour_cos",

    "officer_cases_before_today",
    "department_cases_before_today",
    "procedure_cases_before_today",

    "officer_cases_before_all",
    "department_cases_before_all",
    "procedure_cases_before_all",

    "officer_cases_prev_7d",
    "department_cases_prev_7d",
    "procedure_cases_prev_7d",

    "total_same_day_workload",
    "total_recent_7d_workload",
]

BASE_FEATURES = CAT_FEATURES + NUM_FEATURES

FINAL_SEEDS = [42, 2026, 3407]


# ============================================================
# MODEL LOADING
# ============================================================

def load_json(path: Path):
    with open(path, "r", encoding="utf-8") as file:
        return json.load(file)


def load_catboost_models():
    models = []

    for seed in FINAL_SEEDS:
        path = MODEL_DIR / f"cat_ratio_seed_{seed}.cbm"

        model = CatBoostRegressor()
        model.load_model(str(path))

        models.append(model)

    return models


def load_xgboost_models():
    models = []

    for seed in FINAL_SEEDS:
        path = MODEL_DIR / f"xgb_log_seed_{seed}.json"

        model = XGBRegressor()
        model.load_model(str(path))

        models.append(model)

    return models


MODEL_BUNDLE = load_json(
    MODEL_DIR / "model_bundle.json"
)

PRIORS = joblib.load(
    MODEL_DIR / "historical_priors.joblib"
)

ENCODER = joblib.load(
    MODEL_DIR / "ordinal_encoder.joblib"
)

HISTORY = pd.read_csv(
    MODEL_DIR / "cleaned_dataset.csv",
    encoding="utf-8-sig",
    low_memory=False
)

CAT_MODELS = load_catboost_models()
XGB_MODELS = load_xgboost_models()


# ============================================================
# HISTORY PREPARATION
# ============================================================

HISTORY["Ngày tiếp nhận"] = pd.to_datetime(
    HISTORY["Ngày tiếp nhận"],
    errors="coerce"
)

for col in CAT_FEATURES:
    HISTORY[col] = (
        HISTORY[col]
        .fillna("UNKNOWN")
        .astype(str)
        .str.strip()
        .replace({
            "": "UNKNOWN",
            "nan": "UNKNOWN",
            "None": "UNKNOWN",
        })
    )


# ============================================================
# HELPERS
# ============================================================

def clean_text(value):
    if value is None:
        return "UNKNOWN"

    value = str(value).strip()

    if value == "" or value.lower() in {
        "nan",
        "none"
    }:
        return "UNKNOWN"

    return value


def calculate_sla_hours(received_at, due_at):
    if pd.isna(due_at):
        return np.nan

    hours = (
        due_at - received_at
    ).total_seconds() / 3600

    if (
        hours <= 0
        or hours > MAX_HOURS
    ):
        return np.nan

    return float(hours)


def count_history(
    history,
    column,
    value,
    received_at
):
    mask = (
        (history[column] == value)
        &
        (history["Ngày tiếp nhận"] < received_at)
    )

    return int(mask.sum())


def count_same_day(
    history,
    column,
    value,
    received_at
):
    start = received_at.floor("D")
    end = start + pd.Timedelta(days=1)

    mask = (
        (history[column] == value)
        &
        (history["Ngày tiếp nhận"] >= start)
        &
        (history["Ngày tiếp nhận"] < received_at)
        &
        (history["Ngày tiếp nhận"] < end)
    )

    return int(mask.sum())


def count_previous_7_days(
    history,
    column,
    value,
    received_at
):
    start = (
        received_at
        - pd.Timedelta(days=7)
    )

    mask = (
        (history[column] == value)
        &
        (history["Ngày tiếp nhận"] >= start)
        &
        (history["Ngày tiếp nhận"] < received_at)
    )

    return int(mask.sum())


def read_live_workload(payload):
    workload = payload.get("workload")

    if not isinstance(workload, dict):
        raise ValueError(
            "workload là bắt buộc cho prediction."
        )

    required_keys = [
        "officerCasesBeforeToday",
        "departmentCasesBeforeToday",
        "procedureCasesBeforeToday",
        "officerCasesBeforeAll",
        "departmentCasesBeforeAll",
        "procedureCasesBeforeAll",
        "officerCasesPrevious7Days",
        "departmentCasesPrevious7Days",
        "procedureCasesPrevious7Days",
    ]

    result = {}

    for key in required_keys:
        if key not in workload:
            raise ValueError(
                f"workload.{key} là bắt buộc."
            )

        value = workload[key]

        if (
            isinstance(value, bool)
            or not isinstance(value, int)
            or value < 0
        ):
            raise ValueError(
                f"workload.{key} phải là số nguyên không âm."
            )

        result[key] = value

    return result


# ============================================================
# FEATURE BUILDER
# ============================================================

def build_features(payload):
    procedure = clean_text(
        payload.get("procedureName")
    )

    field = clean_text(
        payload.get("fieldName")
    )

    department = clean_text(
        payload.get("departmentName")
    )

    officer = clean_text(
        payload.get("officerName")
    )

    received_at = pd.to_datetime(
        payload.get("receivedAt"),
        errors="coerce"
    )

    due_at = pd.to_datetime(
        payload.get("dueAt"),
        errors="coerce"
    )

    if pd.isna(received_at):
        raise ValueError(
            "receivedAt không hợp lệ."
        )

    # Production workload is supplied by ASP.NET from SQL Server.
    workload = read_live_workload(
        payload
    )

    raw_sla_hours = calculate_sla_hours(
        received_at,
        due_at
    )

    # Keep the real SLA separately for deadline risk.
    # The model may still need a numeric fallback when the real SLA is missing/invalid.
    risk_sla_hours = (
        None
        if pd.isna(raw_sla_hours)
        else float(raw_sla_hours)
    )

    numeric_medians = PRIORS[
        "numeric_medians"
    ]

    sla_hours = raw_sla_hours

    if pd.isna(sla_hours):
        sla_hours = float(
            numeric_medians.get(
                "sla_hours",
                0
            )
        )

    accept_hour = (
        received_at.hour
        +
        received_at.minute / 60
    )

    accept_dayofweek = (
        received_at.dayofweek
    )

    accept_day = received_at.day
    accept_month = received_at.month

    accept_weekofyear = int(
        received_at.isocalendar().week
    )

    is_weekend = int(
        accept_dayofweek >= 5
    )

    dow_sin = np.sin(
        2 * np.pi
        * accept_dayofweek
        / 7
    )

    dow_cos = np.cos(
        2 * np.pi
        * accept_dayofweek
        / 7
    )

    hour_sin = np.sin(
        2 * np.pi
        * accept_hour
        / 24
    )

    hour_cos = np.cos(
        2 * np.pi
        * accept_hour
        / 24
    )

    officer_same_day = workload[
        "officerCasesBeforeToday"
    ]

    department_same_day = workload[
        "departmentCasesBeforeToday"
    ]

    procedure_same_day = workload[
        "procedureCasesBeforeToday"
    ]

    officer_all = workload[
        "officerCasesBeforeAll"
    ]

    department_all = workload[
        "departmentCasesBeforeAll"
    ]

    procedure_all = workload[
        "procedureCasesBeforeAll"
    ]

    officer_7d = workload[
        "officerCasesPrevious7Days"
    ]

    department_7d = workload[
        "departmentCasesPrevious7Days"
    ]

    procedure_7d = workload[
        "procedureCasesPrevious7Days"
    ]

    row = {
        "Tên thủ tục hành chính":
            procedure,

        "Tên lĩnh vực":
            field,

        "Phòng ban":
            department,

        "Cán bộ xử lý hiện tại":
            officer,

        "sla_hours":
            sla_hours,

        "sla_log":
            np.log1p(sla_hours),

        "accept_hour":
            accept_hour,

        "accept_dayofweek":
            accept_dayofweek,

        "accept_day":
            accept_day,

        "accept_month":
            accept_month,

        "accept_weekofyear":
            accept_weekofyear,

        "is_weekend":
            is_weekend,

        "dow_sin":
            dow_sin,

        "dow_cos":
            dow_cos,

        "hour_sin":
            hour_sin,

        "hour_cos":
            hour_cos,

        "officer_cases_before_today":
            officer_same_day,

        "department_cases_before_today":
            department_same_day,

        "procedure_cases_before_today":
            procedure_same_day,

        "officer_cases_before_all":
            officer_all,

        "department_cases_before_all":
            department_all,

        "procedure_cases_before_all":
            procedure_all,

        "officer_cases_prev_7d":
            officer_7d,

        "department_cases_prev_7d":
            department_7d,

        "procedure_cases_prev_7d":
            procedure_7d,

        "total_same_day_workload":
            (
                officer_same_day
                + department_same_day
                + procedure_same_day
            ),

        "total_recent_7d_workload":
            (
                officer_7d
                + department_7d
                + procedure_7d
            ),
    }

    frame = pd.DataFrame([row])

    # Apply same numeric median fallback
    for col in NUM_FEATURES:
        if frame[col].isna().any():
            median = numeric_medians.get(
                col,
                0
            )

            frame[col] = frame[col].fillna(
                median
            )

    return (
        frame,
        received_at,
        float(sla_hours),
        risk_sla_hours
    )


# ============================================================
# EXPERT 1 - PROCEDURE MEDIAN
# ============================================================

def predict_procedure_median(frame):
    procedure_map = PRIORS[
        "procedure_map"
    ]

    global_median = float(
        PRIORS["global_median"]
    )

    procedure = frame.iloc[0][
        "Tên thủ tục hành chính"
    ]

    return float(
        procedure_map.get(
            procedure,
            global_median
        )
    )


# ============================================================
# EXPERT 2 - HIERARCHICAL PRIOR
# ============================================================

def predict_hierarchical_prior(frame):
    state = PRIORS[
        "hierarchical_prior"
    ]

    row = frame.iloc[0]

    department = row["Phòng ban"]
    procedure = row[
        "Tên thủ tục hành chính"
    ]
    officer = row[
        "Cán bộ xử lý hiện tại"
    ]

    global_median = float(
        state["global"]
    )

    # Department
    dept_med = float(
        state["dept_med"].get(
            department,
            global_median
        )
    )

    dept_n = float(
        state["dept_n"].get(
            department,
            0
        )
    )

    w_dept = (
        dept_n
        /
        (dept_n + 30.0)
    )

    dept_prior = (
        w_dept * dept_med
        +
        (1 - w_dept)
        * global_median
    )

    # Procedure
    proc_med = state[
        "proc_med"
    ].get(
        procedure
    )

    proc_n = float(
        state["proc_n"].get(
            procedure,
            0
        )
    )

    if proc_med is None:
        proc_med = dept_prior
    else:
        proc_med = float(proc_med)

    w_proc = (
        proc_n
        /
        (proc_n + 12.0)
    )

    proc_prior = (
        w_proc * proc_med
        +
        (1 - w_proc)
        * dept_prior
    )

    # Procedure + officer
    key = (
        procedure,
        officer
    )

    po_med = state[
        "po_med"
    ].get(key)

    po_n = float(
        state["po_n"].get(
            key,
            0
        )
    )

    if po_med is None:
        po_med = proc_prior
    else:
        po_med = float(po_med)

    w_po = (
        po_n
        /
        (po_n + 8.0)
    )

    result = (
        w_po * po_med
        +
        (1 - w_po)
        * proc_prior
    )

    return float(result)


# ============================================================
# CATBOOST RATIO CAP
# ============================================================

def calculate_ratio_cap():
    history = HISTORY.copy()

    if "processing_hours" not in history.columns:
        return 2.0

    pretest = history[
        history["Ngày tiếp nhận"]
        < FINAL_TEST_START
    ].copy()

    test = history[
        history["Ngày tiếp nhận"]
        >= FINAL_TEST_START
    ].copy()

    if (
        len(pretest) < 400
        or len(test) < 50
    ):
        split = int(
            len(history) * 0.80
        )

        pretest = (
            history
            .iloc[:split]
            .copy()
        )

    sla_median = PRIORS[
        "numeric_medians"
    ].get(
        "sla_hours",
        1
    )

    pretest["sla_hours"] = (
        pretest["sla_hours"]
        .fillna(sla_median)
    )

    ratio = (
        pretest["processing_hours"]
        .astype(float)
        .to_numpy()
        /
        np.maximum(
            pretest["sla_hours"]
            .astype(float)
            .to_numpy(),
            1.0
        )
    )

    return float(
        max(
            2.0,
            np.quantile(
                ratio,
                0.995
            )
        )
    )


RATIO_CAP = calculate_ratio_cap()


# ============================================================
# EXPERT 3 - CATBOOST SLA RATIO
# ============================================================

def predict_cat_ratio(frame):
    predictions = []

    sla_hours = float(
        frame.iloc[0]["sla_hours"]
    )

    for model in CAT_MODELS:
        ratio = float(
            model.predict(
                frame[BASE_FEATURES]
            )[0]
        )

        ratio = float(
            np.clip(
                ratio,
                0,
                RATIO_CAP
            )
        )

        prediction = (
            ratio
            * sla_hours
        )

        predictions.append(
            np.clip(
                prediction,
                0,
                MAX_HOURS
            )
        )

    return float(
        np.mean(predictions)
    )


# ============================================================
# EXPERT 4 - XGBOOST LOG DURATION
# ============================================================

def create_numeric_matrix(frame):
    cat_matrix = ENCODER.transform(
        frame[CAT_FEATURES]
    )

    numeric_matrix = (
        frame[NUM_FEATURES]
        .astype(np.float32)
        .to_numpy()
    )

    return np.hstack([
        cat_matrix,
        numeric_matrix
    ]).astype(
        np.float32
    )


def predict_xgb_log(frame):
    matrix = create_numeric_matrix(
        frame
    )

    predictions = []

    for model in XGB_MODELS:
        pred_log = float(
            model.predict(
                matrix
            )[0]
        )

        prediction = float(
            np.expm1(
                pred_log
            )
        )

        predictions.append(
            np.clip(
                prediction,
                0,
                MAX_HOURS
            )
        )

    return float(
        np.mean(predictions)
    )


# ============================================================
# ENSEMBLE
# ============================================================

def ensemble_predictions(predictions):
    selected = MODEL_BUNDLE.get(
        "selected_meta_model"
    )

    if selected != "STATIC_ENSEMBLE":
        raise RuntimeError(
            "Model hiện tại không phải STATIC_ENSEMBLE."
        )

    weights = MODEL_BUNDLE.get(
        "final_static_weights"
    )

    if not weights:
        raise RuntimeError(
            "Không tìm thấy final_static_weights "
            "trong model_bundle.json."
        )

    result = 0.0

    for expert, weight in weights.items():
        if expert not in predictions:
            continue

        result += (
            float(weight)
            * float(
                predictions[expert]
            )
        )

    return float(
        np.clip(
            result,
            0,
            MAX_HOURS
        )
    )


# ============================================================
# UNCERTAINTY + RISK
# ============================================================

def build_uncertainty(prediction):
    uncertainty = MODEL_BUNDLE[
        "uncertainty"
    ]

    q80 = float(
        uncertainty["q80"]
    )

    q90 = float(
        uncertainty["q90"]
    )

    scale = prediction + 24.0

    lower80 = max(
        0,
        prediction
        - q80 * scale
    )

    upper80 = min(
        MAX_HOURS,
        prediction
        + q80 * scale
    )

    lower90 = max(
        0,
        prediction
        - q90 * scale
    )

    upper90 = min(
        MAX_HOURS,
        prediction
        + q90 * scale
    )

    return {
        "lower80": float(lower80),
        "upper80": float(upper80),
        "lower90": float(lower90),
        "upper90": float(upper90),
    }

def calculate_risk(
    prediction,
    upper80,
    upper90,
    sla_hours
):
    if (
        sla_hours is None
        or not np.isfinite(sla_hours)
        or sla_hours <= 0
    ):
        return "UNKNOWN"

    prediction_ratio = prediction / sla_hours
    upper80_ratio = upper80 / sla_hours
    upper90_ratio = upper90 / sla_hours

    risk_score = (
        0.50 * prediction_ratio
        + 0.30 * upper80_ratio
        + 0.20 * upper90_ratio
    )

    if risk_score >= 1.00:
        return "CRITICAL"

    if risk_score >= 0.80:
        return "HIGH"

    if risk_score >= 0.55:
        return "MEDIUM"

    return "LOW"



# ============================================================
# MAIN PREDICTION
# ============================================================

def predict(payload):
    (
        frame,
        received_at,
        sla_hours,
        risk_sla_hours
    ) = build_features(
        payload
    )

    pred_proc = (
        predict_procedure_median(
            frame
        )
    )

    pred_prior = (
        predict_hierarchical_prior(
            frame
        )
    )

    pred_cat = (
        predict_cat_ratio(
            frame
        )
    )

    pred_xgb = (
        predict_xgb_log(
            frame
        )
    )

    expert_predictions = {
        "PROC":
            pred_proc,

        "PRIOR":
            pred_prior,

        "CAT_RATIO":
            pred_cat,

        "XGB_LOG":
            pred_xgb,
    }

    final_prediction = (
        ensemble_predictions(
            expert_predictions
        )
    )

    intervals = build_uncertainty(
        final_prediction
    )

    predicted_completion = (
        received_at
        +
        pd.to_timedelta(
            final_prediction,
            unit="h"
        )
    )

    predicted_completion_p90 = (
        received_at
        +
        pd.to_timedelta(
            intervals["upper90"],
            unit="h"
        )
    )

    risk = calculate_risk(
        final_prediction,
        intervals["upper80"],
        intervals["upper90"],
        risk_sla_hours
    )

    print(
        "RISK DEBUG: "
        f"prediction={final_prediction:.2f}, "
        f"upper80={intervals['upper80']:.2f}, "
        f"upper90={intervals['upper90']:.2f}, "
        f"sla_hours={risk_sla_hours if risk_sla_hours is not None else 'UNKNOWN'}, "
        f"risk={risk}",
        file=sys.stderr
    )

    return {
        "predictedProcessingHours":
            round(
                final_prediction,
                4
            ),

        "predictionLower80":
            round(
                intervals["lower80"],
                4
            ),

        "predictionUpper80":
            round(
                intervals["upper80"],
                4
            ),

        "predictionLower90":
            round(
                intervals["lower90"],
                4
            ),

        "predictionUpper90":
            round(
                intervals["upper90"],
                4
            ),

        "predictedCompletionTime":
            predicted_completion.isoformat(),

        "predictedCompletionP90":
            predicted_completion_p90.isoformat(),

        "deadlineRisk":
            risk,

        "experts": {
            key: round(value, 4)
            for key, value
            in expert_predictions.items()
        }
    }


# ============================================================
# DEMO
# ============================================================

def create_demo_payload():
    row = (
        HISTORY
        .sort_values(
            "Ngày tiếp nhận"
        )
        .iloc[-1]
    )

    received = (
        pd.Timestamp.now()
        .floor("s")
    )

    historical_sla = float(
        row.get(
            "sla_hours",
            24
        )
    )

    if (
        np.isnan(historical_sla)
        or historical_sla <= 0
    ):
        historical_sla = 24

    due = (
        received
        +
        pd.to_timedelta(
            historical_sla,
            unit="h"
        )
    )

    payload = {
        "procedureName":
            clean_text(
                row[
                    "Tên thủ tục hành chính"
                ]
            ),

        "fieldName":
            clean_text(
                row[
                    "Tên lĩnh vực"
                ]
            ),

        "departmentName":
            clean_text(
                row["Phòng ban"]
            ),

        "officerName":
            clean_text(
                row[
                    "Cán bộ xử lý hiện tại"
                ]
            ),

        "receivedAt":
            received.isoformat(),

        "dueAt":
            due.isoformat(),
    }

    # Demo mode intentionally derives workload from the training reference CSV.
    payload["workload"] = {
        "officerCasesBeforeToday":
            count_same_day(
                HISTORY,
                "Cán bộ xử lý hiện tại",
                payload["officerName"],
                received
            ),

        "departmentCasesBeforeToday":
            count_same_day(
                HISTORY,
                "Phòng ban",
                payload["departmentName"],
                received
            ),

        "procedureCasesBeforeToday":
            count_same_day(
                HISTORY,
                "Tên thủ tục hành chính",
                payload["procedureName"],
                received
            ),

        "officerCasesBeforeAll":
            count_history(
                HISTORY,
                "Cán bộ xử lý hiện tại",
                payload["officerName"],
                received
            ),

        "departmentCasesBeforeAll":
            count_history(
                HISTORY,
                "Phòng ban",
                payload["departmentName"],
                received
            ),

        "procedureCasesBeforeAll":
            count_history(
                HISTORY,
                "Tên thủ tục hành chính",
                payload["procedureName"],
                received
            ),

        "officerCasesPrevious7Days":
            count_previous_7_days(
                HISTORY,
                "Cán bộ xử lý hiện tại",
                payload["officerName"],
                received
            ),

        "departmentCasesPrevious7Days":
            count_previous_7_days(
                HISTORY,
                "Phòng ban",
                payload["departmentName"],
                received
            ),

        "procedureCasesPrevious7Days":
            count_previous_7_days(
                HISTORY,
                "Tên thủ tục hành chính",
                payload["procedureName"],
                received
            ),
    }

    return payload


# ============================================================
# CLI
# ============================================================

if __name__ == "__main__":
    try:
        # ========================================================
        # DEMO MODE
        # ========================================================
        if (
            len(sys.argv) > 1
            and sys.argv[1] == "--demo"
        ):
            input_payload = create_demo_payload()

            print(
                "INPUT:",
                json.dumps(
                    input_payload,
                    ensure_ascii=False,
                    indent=2
                ),
                file=sys.stderr
            )

        # ========================================================
        # PRODUCTION MODE - JSON FROM ASP.NET STDIN
        # ========================================================
        else:
            raw = sys.stdin.read()

            # Debug tạm thời: stderr không làm bẩn stdout JSON.
            print(
                f"STDIN RAW: {raw!r}",
                file=sys.stderr
            )

            if raw is None or not raw.strip():
                raise ValueError(
                    "ASP.NET không gửi JSON input sang predict.py."
                )

            # Loại bỏ UTF-8 BOM và whitespace đầu/cuối nếu có.
            raw = raw.lstrip("\ufeff").strip()

            try:
                input_payload = json.loads(raw)
            except json.JSONDecodeError as error:
                print(
                    f"JSON DECODE ERROR: {error}",
                    file=sys.stderr
                )
                print(
                    f"INVALID JSON: {raw!r}",
                    file=sys.stderr
                )
                raise ValueError(
                    f"JSON input không hợp lệ: {error}"
                ) from error

            if not isinstance(input_payload, dict):
                raise ValueError(
                    "JSON input phải là một object."
                )

        # ========================================================
        # PREDICT
        # ========================================================
        result = predict(input_payload)

        # stdout chỉ chứa JSON để ASP.NET deserialize.
        print(
            json.dumps(
                result,
                ensure_ascii=False
            )
        )

    except Exception as error:
        # Chi tiết debug chỉ ghi stderr.
        print(
            f"AI PREDICTION ERROR: {type(error).__name__}: {error}",
            file=sys.stderr
        )

        # stdout vẫn trả JSON error để ASP.NET đọc được.
        print(
            json.dumps(
                {
                    "error": str(error)
                },
                ensure_ascii=False
            )
        )

        sys.exit(1)
