import { useEffect, useRef, useState } from "react";
import {
  FileSpreadsheet,
  LoaderCircle,
  RotateCcw,
  UploadCloud,
} from "lucide-react";

import { postFormData } from "../services/api";
import { getLastImportSync } from "../services/importService";

import "./ImportPage.css";

const acceptedExtensions = [".xlsx", ".csv"];
function formatFileSize(bytes) {
  if (!Number.isFinite(bytes)) return "—";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function formatModifiedDate(timestamp) {
  if (!timestamp) return "—";
  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(timestamp));
}

function getExtension(filename) {
  const dotIndex = filename.lastIndexOf(".");
  return dotIndex >= 0 ? filename.slice(dotIndex).toLowerCase() : "";
}

function ImportPage() {
  const fileInputRef = useRef(null);
  const [selectedFile, setSelectedFile] = useState(null);
  const [fileError, setFileError] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [result, setResult] = useState(null);
  const [lastSync, setLastSync] = useState(null);
  const [syncError, setSyncError] = useState("");

  function loadLastSync() {
    getLastImportSync({ force: true })
      .then((data) => { setLastSync(data); setSyncError(""); })
      .catch((loadError) => setSyncError(loadError.message || "Không thể tải thời điểm cập nhật dữ liệu."));
  }

  useEffect(() => {
    let isCurrent = true;
    getLastImportSync()
      .then((data) => { if (isCurrent) setLastSync(data); })
      .catch((loadError) => { if (isCurrent) setSyncError(loadError.message || "Không thể tải thời điểm cập nhật dữ liệu."); });
    return () => { isCurrent = false; };
  }, []);

  function handleFileChange(event) {
    const [file] = event.target.files;
    event.target.value = "";

    if (!file) return;

    if (!acceptedExtensions.includes(getExtension(file.name))) {
      setSelectedFile(null);
      setFileError("Chỉ hỗ trợ file .xlsx hoặc .csv.");
      return;
    }

    setSelectedFile(file);
    setFileError("");
    setError("");
    setResult(null);
  }

  function resetImport() {
    setSelectedFile(null);
    setFileError("");
    setError("");
    setResult(null);
  }

  async function handleImport() {
    if (!selectedFile || loading) return;

    const formData = new FormData();
    formData.append("file", selectedFile);

    setLoading(true);
    setError("");
    setResult(null);

    try {
      const importResult = await postFormData("/import/cases", formData);
      setResult(importResult);
      loadLastSync();
    } catch (importError) {
      setError(importError.message || "Không thể import dữ liệu.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <section className="data-transfer-page">
      <div className="data-transfer-page__heading">
        <h1>Import dữ liệu</h1>
        <p>Chuẩn bị file dữ liệu và thiết lập điều kiện xuất báo cáo.</p>
      </div>
      {lastSync?.lastUpdatedAt && <p className="data-sync-status">Cập nhật dữ liệu lần cuối: {formatModifiedDate(new Date(lastSync.lastUpdatedAt).getTime())}</p>}
      {syncError && <p className="transfer-message transfer-message--error" role="alert">{syncError}</p>}

      <article className="transfer-card">
        <header className="transfer-card__header">
          <div>
            <span className="transfer-card__icon">
              <UploadCloud size={19} />
            </span>
            <div>
              <h2>Import dữ liệu</h2>
              <p>Hỗ trợ file Excel (.xlsx) và CSV.</p>
            </div>
          </div>
          {selectedFile && (
            <button
              className="transfer-button transfer-button--secondary"
              type="button"
              onClick={resetImport}
            >
              <RotateCcw size={14} /> Chọn lại
            </button>
          )}
        </header>

        <div className="transfer-card__body">
          <button
            className="file-dropzone"
            disabled={loading}
            type="button"
            onClick={() => fileInputRef.current?.click()}
          >
            <FileSpreadsheet size={29} />
            <strong>Chọn file dữ liệu</strong>
            <span>.xlsx hoặc .csv</span>
          </button>
          <input
            ref={fileInputRef}
            accept=".xlsx,.csv"
            className="transfer-file-input"
            type="file"
            onChange={handleFileChange}
          />

          {selectedFile && (
            <div className="selected-file-info">
              <FileSpreadsheet size={18} />
              <div>
                <strong title={selectedFile.name}>{selectedFile.name}</strong>
                <span>
                  {formatFileSize(selectedFile.size)} ·{" "}
                  {getExtension(selectedFile.name).slice(1).toUpperCase()} · Sửa
                  lần cuối {formatModifiedDate(selectedFile.lastModified)}
                </span>
              </div>
              <span className="transfer-state transfer-state--selected">
                Đã chọn
              </span>
            </div>
          )}
          {fileError && (
            <p
              className="transfer-message transfer-message--error"
              role="alert"
            >
              {fileError}
            </p>
          )}
          {error && (
            <p
              className="transfer-message transfer-message--error"
              role="alert"
            >
              {error}
            </p>
          )}
        </div>

        <div className="preview-section">
          <div className="preview-section__heading">
            <div>
              <h3>Xem trước dữ liệu</h3>
              <p>Kết quả import và các dòng lỗi sẽ hiển thị tại đây.</p>
            </div>
            <button
              className="transfer-button transfer-button--primary"
              disabled={!selectedFile || loading}
              type="button"
              onClick={handleImport}
            >
              {loading ? (
                <LoaderCircle className="transfer-spinner" size={14} />
              ) : (
                <UploadCloud size={14} />
              )}
              {loading ? "Đang import..." : "Import dữ liệu"}
            </button>
          </div>
          {result ? (
            <div className="import-result">
              <div className="import-result__summary">
                <div>
                  <span>Tổng dòng</span>
                  <strong>{result.totalRows}</strong>
                </div>
                <div>
                  <span>Thêm mới</span>
                  <strong>{result.insertedCount}</strong>
                </div>
                <div>
                  <span>Cập nhật</span>
                  <strong>{result.updatedCount}</strong>
                </div>
                <div>
                  <span>Không đổi</span>
                  <strong>{result.unchangedCount}</strong>
                </div>
                <div>
                  <span>Thất bại</span>
                  <strong>{result.failedCount}</strong>
                </div>
              </div>
              {result.errors?.length > 0 ? (
                <div className="preview-table-wrap">
                  <table className="preview-table">
                    <thead>
                      <tr>
                        <th>Row</th>
                        <th>Mã hồ sơ</th>
                        <th>Lỗi</th>
                      </tr>
                    </thead>
                    <tbody>
                      {result.errors.map((item, index) => (
                        <tr
                          key={`${item.row}-${item.externalCaseCode || "empty"}-${index}`}
                        >
                          <td>{item.row}</td>
                          <td>{item.externalCaseCode || "—"}</td>
                          <td>{item.message}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="transfer-message transfer-message--success">
                  Tất cả hồ sơ đã được import thành công.
                </p>
              )}
            </div>
          ) : (
            <div className="preview-empty-state">
              <FileSpreadsheet size={28} />
              <strong>Chưa có kết quả import</strong>
              <span>
                {selectedFile
                  ? "Nhấn Import dữ liệu để gửi file lên hệ thống."
                  : "Chọn file để chuẩn bị import dữ liệu."}
              </span>
            </div>
          )}
        </div>
      </article>
    </section>
  );
}

export default ImportPage;
