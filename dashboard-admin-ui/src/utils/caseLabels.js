const caseStatusLabels = {
  1: "Mới tiếp nhận",
  2: "Đang xử lý",
  3: "Chờ xử lý",
  4: "Đã hoàn thành",
  5: "Quá hạn",
  6: "Đã hủy",
};

const caseStatusBadgeKeys = {
  1: "new",
  2: "processing",
  3: "upcoming",
  4: "completed",
  5: "overdue",
  6: "overdue",
};

const assignmentStatusLabels = {
  1: "Đã phân công",
  2: "Đang xử lý",
  3: "Đã hoàn thành",
  4: "Đã hủy",
};

const caseActionTypeLabels = {
  1: "Tạo hồ sơ",
  2: "Phân công",
  3: "Phân công lại",
  4: "Thay đổi trạng thái",
  5: "Cập nhật hồ sơ",
  6: "Hoàn thành hồ sơ",
  7: "Hủy hồ sơ",
};

export function getCaseStatusLabel(status) {
  return caseStatusLabels[status] ?? "Không xác định";
}

export function getCaseStatusBadgeKey(status) {
  return caseStatusBadgeKeys[status] ?? "new";
}

export function getAssignmentStatusLabel(status) {
  return assignmentStatusLabels[status] ?? "Không xác định";
}

export function getCaseActionTypeLabel(actionType) {
  return caseActionTypeLabels[actionType] ?? "Hoạt động khác";
}
