export const departments = [
  { id: "dept-justice", name: "Phòng Tư pháp" },
  { id: "dept-urban", name: "Phòng Quản lý đô thị" },
  { id: "dept-economy", name: "Phòng Kinh tế" },
  { id: "dept-environment", name: "Phòng Tài nguyên và Môi trường" },
];

export const fields = [
  { id: "field-civil", name: "Hộ tịch", departmentId: "dept-justice" },
  { id: "field-construction", name: "Xây dựng", departmentId: "dept-urban" },
  { id: "field-transport", name: "Giao thông vận tải", departmentId: "dept-urban" },
  { id: "field-business", name: "Đăng ký kinh doanh", departmentId: "dept-economy" },
  { id: "field-land", name: "Đất đai và môi trường", departmentId: "dept-environment" },
];

export const procedures = [
  { id: "proc-birth", name: "Đăng ký khai sinh", fieldId: "field-civil", processingDays: 3 },
  { id: "proc-marriage", name: "Đăng ký kết hôn", fieldId: "field-civil", processingDays: 5 },
  { id: "proc-build-new", name: "Cấp giấy phép xây dựng", fieldId: "field-construction", processingDays: 15 },
  { id: "proc-build-adjust", name: "Điều chỉnh giấy phép xây dựng", fieldId: "field-construction", processingDays: 10 },
  { id: "proc-transport", name: "Cấp phép kinh doanh vận tải", fieldId: "field-transport", processingDays: 10 },
  { id: "proc-business", name: "Đăng ký hộ kinh doanh", fieldId: "field-business", processingDays: 5 },
  { id: "proc-business-change", name: "Thay đổi nội dung đăng ký hộ kinh doanh", fieldId: "field-business", processingDays: 3 },
  { id: "proc-land", name: "Đăng ký biến động đất đai", fieldId: "field-land", processingDays: 15 },
  { id: "proc-environment", name: "Xác nhận hồ sơ môi trường", fieldId: "field-land", processingDays: 10 },
];

export const users = [
  { id: "user-lan", fullName: "Nguyễn Thị Lan", departmentId: "dept-justice", email: "lan.nguyen@hanhchinh.vn", phone: "0901 234 501" },
  { id: "user-minh", fullName: "Trần Quốc Minh", departmentId: "dept-justice", email: "minh.tran@hanhchinh.vn", phone: "0901 234 502" },
  { id: "user-binh", fullName: "Trần Văn Bình", departmentId: "dept-urban", email: "binh.tran@hanhchinh.vn", phone: "0901 234 503" },
  { id: "user-anh", fullName: "Nguyễn Minh Anh", departmentId: "dept-urban", email: "anh.nguyen@hanhchinh.vn", phone: "0901 234 504" },
  { id: "user-hung", fullName: "Phạm Văn Hùng", departmentId: "dept-economy", email: "hung.pham@hanhchinh.vn", phone: "0901 234 505" },
  { id: "user-thao", fullName: "Lê Thu Thảo", departmentId: "dept-economy", email: "thao.le@hanhchinh.vn", phone: "0901 234 506" },
  { id: "user-nam", fullName: "Đỗ Quang Nam", departmentId: "dept-environment", email: "nam.do@hanhchinh.vn", phone: "0901 234 507" },
  { id: "user-ha", fullName: "Vũ Thanh Hà", departmentId: "dept-environment", email: "ha.vu@hanhchinh.vn", phone: "0901 234 508" },
];

export const caseStatuses = [
  "Mới tiếp nhận",
  "Đang xử lý",
  "Sắp hạn",
  "Quá hạn",
  "Hoàn thành",
];

export const cases = [
  { id: "case-001", caseCode: "HS-2026-0187", caseName: "Cấp phép xây dựng nhà ở ông Nguyễn Văn An", procedureId: "proc-build-new", assignedUserId: "user-binh", receivedDate: "2026-08-06", dueDate: "2026-08-21T17:00:00", appointmentReturnDate: "2026-08-22T08:00:00", completedDate: "", status: "Sắp hạn", note: "Hồ sơ đã đầy đủ, đang thẩm định bản vẽ." },
  { id: "case-002", caseCode: "HS-2026-0176", caseName: "Điều chỉnh giấy phép xây dựng công trình Minh Phát", procedureId: "proc-build-adjust", assignedUserId: "user-anh", receivedDate: "2026-08-04", dueDate: "2026-08-14T16:30:00", appointmentReturnDate: "2026-08-15T08:00:00", completedDate: "", status: "Đang xử lý", note: "Đang đối chiếu nội dung điều chỉnh." },
  { id: "case-003", caseCode: "HS-2026-0162", caseName: "Đăng ký hộ kinh doanh Nguyễn Gia", procedureId: "proc-business", assignedUserId: "user-hung", receivedDate: "2026-07-28", dueDate: "2026-08-02T17:00:00", appointmentReturnDate: "2026-08-03T09:00:00", completedDate: "", status: "Quá hạn", note: "Chờ bổ sung giấy tờ chứng minh địa điểm kinh doanh." },
  { id: "case-004", caseCode: "HS-2026-0158", caseName: "Cấp phép vận tải Công ty An Phú", procedureId: "proc-transport", assignedUserId: "user-anh", receivedDate: "2026-08-03", dueDate: "2026-08-13T15:45:00", appointmentReturnDate: "2026-08-14T08:00:00", completedDate: "", status: "Sắp hạn", note: "Đang kiểm tra điều kiện phương tiện." },
  { id: "case-005", caseCode: "HS-2026-0149", caseName: "Đăng ký biến động thửa đất số 128", procedureId: "proc-land", assignedUserId: "user-ha", receivedDate: "2026-07-30", dueDate: "2026-08-14T17:00:00", appointmentReturnDate: "2026-08-15T08:30:00", completedDate: "", status: "Đang xử lý", note: "Đã chuyển bộ phận đo đạc xác minh." },
  { id: "case-006", caseCode: "HS-2026-0136", caseName: "Xác nhận môi trường cơ sở Thành Công", procedureId: "proc-environment", assignedUserId: "user-nam", receivedDate: "2026-07-20", dueDate: "2026-07-30T17:00:00", appointmentReturnDate: "2026-07-31T08:00:00", completedDate: "2026-07-29", status: "Hoàn thành", note: "Đã trả kết quả cho tổ chức." },
  { id: "case-007", caseCode: "HS-2026-0128", caseName: "Đăng ký khai sinh cho Nguyễn Minh Khôi", procedureId: "proc-birth", assignedUserId: "user-lan", receivedDate: "2026-08-07", dueDate: "2026-08-07T23:30:00", appointmentReturnDate: "2026-08-07T23:45:00", completedDate: "", status: "Mới tiếp nhận", note: "Hồ sơ tiếp nhận trực tuyến." },
  { id: "case-008", caseCode: "HS-2026-0115", caseName: "Đăng ký kết hôn Trần Văn Long - Lê Mai", procedureId: "proc-marriage", assignedUserId: "user-minh", receivedDate: "2026-08-05", dueDate: "2026-08-10T10:30:00", appointmentReturnDate: "2026-08-11T08:00:00", completedDate: "", status: "Đang xử lý", note: "Đang xác minh thông tin cư trú." },
  { id: "case-009", caseCode: "HS-2026-0107", caseName: "Thay đổi ngành nghề hộ kinh doanh Hoàng Hà", procedureId: "proc-business-change", assignedUserId: "user-thao", receivedDate: "2026-08-06", dueDate: "2026-08-09T11:15:00", appointmentReturnDate: "2026-08-10T08:30:00", completedDate: "", status: "Sắp hạn", note: "Chờ lãnh đạo phê duyệt." },
  { id: "case-010", caseCode: "HS-2026-0094", caseName: "Cấp phép xây dựng nhà ở bà Phạm Thu", procedureId: "proc-build-new", assignedUserId: "user-binh", receivedDate: "2026-07-15", dueDate: "2026-07-30T16:00:00", appointmentReturnDate: "2026-07-31T08:00:00", completedDate: "", status: "Quá hạn", note: "Cần làm rõ chỉ giới xây dựng." },
  { id: "case-011", caseCode: "HS-2026-0082", caseName: "Đăng ký biến động đất đai hộ ông Lâm", procedureId: "proc-land", assignedUserId: "user-nam", receivedDate: "2026-07-18", dueDate: "2026-08-02T17:00:00", appointmentReturnDate: "2026-08-03T08:00:00", completedDate: "2026-08-01", status: "Hoàn thành", note: "Đã cập nhật giấy chứng nhận." },
  { id: "case-012", caseCode: "HS-2026-0074", caseName: "Đăng ký hộ kinh doanh Quán Mộc", procedureId: "proc-business", assignedUserId: "user-hung", receivedDate: "2026-08-05", dueDate: "2026-08-10T14:00:00", appointmentReturnDate: "2026-08-11T08:00:00", completedDate: "", status: "Đang xử lý", note: "Hồ sơ hợp lệ." },
  { id: "case-013", caseCode: "HS-2026-0068", caseName: "Đăng ký khai sinh cho Lê Gia Hân", procedureId: "proc-birth", assignedUserId: "user-lan", receivedDate: "2026-08-04", dueDate: "2026-08-07T09:00:00", appointmentReturnDate: "2026-08-07T10:00:00", completedDate: "2026-08-06", status: "Hoàn thành", note: "Đã trả kết quả trực tuyến." },
  { id: "case-014", caseCode: "HS-2026-0053", caseName: "Xác nhận môi trường xưởng sản xuất Đại Nam", procedureId: "proc-environment", assignedUserId: "user-ha", receivedDate: "2026-08-07", dueDate: "2026-08-17T17:00:00", appointmentReturnDate: "2026-08-18T08:00:00", completedDate: "", status: "Mới tiếp nhận", note: "Đang kiểm tra thành phần hồ sơ." },
];

export const caseHistories = [
  { id: "history-001", caseId: "case-001", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-06T08:15:23", note: "Hồ sơ được tiếp nhận tại bộ phận một cửa." },
  { id: "history-002", caseId: "case-001", status: "Phân công chuyên viên", createdAt: "2026-08-06T08:32:10", note: "Phân công Trần Văn Bình xử lý." },
  { id: "history-003", caseId: "case-001", status: "Đang xử lý", createdAt: "2026-08-06T09:05:47", note: "Bắt đầu thẩm định hồ sơ." },
  { id: "history-004", caseId: "case-002", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-04T08:08:14", note: "Đã kiểm tra thành phần hồ sơ." },
  { id: "history-005", caseId: "case-002", status: "Phân công chuyên viên", createdAt: "2026-08-04T08:25:42", note: "Phân công Nguyễn Minh Anh xử lý." },
  { id: "history-006", caseId: "case-002", status: "Đang xử lý", createdAt: "2026-08-04T10:12:08", note: "Đang đối chiếu nội dung điều chỉnh." },
  { id: "history-007", caseId: "case-003", status: "Tiếp nhận hồ sơ", createdAt: "2026-07-28T08:20:31", note: "Hồ sơ được tiếp nhận trực tiếp." },
  { id: "history-008", caseId: "case-003", status: "Phân công chuyên viên", createdAt: "2026-07-28T08:41:16", note: "Phân công Phạm Văn Hùng xử lý." },
  { id: "history-009", caseId: "case-003", status: "Yêu cầu bổ sung", createdAt: "2026-07-30T14:18:52", note: "Yêu cầu bổ sung giấy tờ về địa điểm kinh doanh." },
  { id: "history-010", caseId: "case-004", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-03T09:04:27", note: "Hồ sơ được tiếp nhận trực tuyến." },
  { id: "history-011", caseId: "case-004", status: "Phân công chuyên viên", createdAt: "2026-08-03T09:22:05", note: "Phân công Nguyễn Minh Anh xử lý." },
  { id: "history-012", caseId: "case-004", status: "Đang xử lý", createdAt: "2026-08-03T10:01:39", note: "Kiểm tra điều kiện phương tiện." },
  { id: "history-013", caseId: "case-005", status: "Tiếp nhận hồ sơ", createdAt: "2026-07-30T08:10:18", note: "Đã tiếp nhận hồ sơ đất đai." },
  { id: "history-014", caseId: "case-005", status: "Phân công chuyên viên", createdAt: "2026-07-30T08:37:44", note: "Phân công Vũ Thanh Hà xử lý." },
  { id: "history-015", caseId: "case-005", status: "Đang xử lý", createdAt: "2026-07-30T09:16:20", note: "Chuyển bộ phận đo đạc xác minh." },
  { id: "history-016", caseId: "case-006", status: "Tiếp nhận hồ sơ", createdAt: "2026-07-20T08:03:12", note: "Đã tiếp nhận hồ sơ môi trường." },
  { id: "history-017", caseId: "case-006", status: "Đang xử lý", createdAt: "2026-07-20T10:26:48", note: "Thẩm định nội dung hồ sơ." },
  { id: "history-018", caseId: "case-006", status: "Hoàn thành", createdAt: "2026-07-29T15:42:06", note: "Đã trả kết quả cho tổ chức." },
  { id: "history-019", caseId: "case-007", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-07T08:12:35", note: "Hồ sơ được tiếp nhận trực tuyến." },
  { id: "history-020", caseId: "case-007", status: "Phân công chuyên viên", createdAt: "2026-08-07T08:29:11", note: "Phân công Nguyễn Thị Lan xử lý." },
  { id: "history-021", caseId: "case-008", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-05T09:17:03", note: "Đã tiếp nhận hồ sơ đăng ký kết hôn." },
  { id: "history-022", caseId: "case-008", status: "Phân công chuyên viên", createdAt: "2026-08-05T09:34:28", note: "Phân công Trần Quốc Minh xử lý." },
  { id: "history-023", caseId: "case-008", status: "Đang xử lý", createdAt: "2026-08-05T10:06:54", note: "Đang xác minh thông tin cư trú." },
  { id: "history-024", caseId: "case-009", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-06T08:42:16", note: "Hồ sơ được tiếp nhận tại bộ phận một cửa." },
  { id: "history-025", caseId: "case-009", status: "Phân công chuyên viên", createdAt: "2026-08-06T09:03:37", note: "Phân công Lê Thu Thảo xử lý." },
  { id: "history-026", caseId: "case-009", status: "Đang xử lý", createdAt: "2026-08-06T09:38:22", note: "Chờ lãnh đạo phê duyệt." },
  { id: "history-027", caseId: "case-010", status: "Tiếp nhận hồ sơ", createdAt: "2026-07-15T08:25:09", note: "Đã tiếp nhận hồ sơ xây dựng." },
  { id: "history-028", caseId: "case-010", status: "Phân công chuyên viên", createdAt: "2026-07-15T08:46:33", note: "Phân công Trần Văn Bình xử lý." },
  { id: "history-029", caseId: "case-010", status: "Yêu cầu bổ sung", createdAt: "2026-07-17T14:11:57", note: "Yêu cầu làm rõ chỉ giới xây dựng." },
  { id: "history-030", caseId: "case-011", status: "Tiếp nhận hồ sơ", createdAt: "2026-07-18T08:15:40", note: "Đã tiếp nhận hồ sơ đất đai." },
  { id: "history-031", caseId: "case-011", status: "Đang xử lý", createdAt: "2026-07-18T10:22:14", note: "Kiểm tra thông tin biến động." },
  { id: "history-032", caseId: "case-011", status: "Hoàn thành", createdAt: "2026-08-01T14:36:29", note: "Đã cập nhật giấy chứng nhận." },
  { id: "history-033", caseId: "case-012", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-05T13:07:21", note: "Đã tiếp nhận hồ sơ kinh doanh." },
  { id: "history-034", caseId: "case-012", status: "Phân công chuyên viên", createdAt: "2026-08-05T13:25:46", note: "Phân công Phạm Văn Hùng xử lý." },
  { id: "history-035", caseId: "case-012", status: "Đang xử lý", createdAt: "2026-08-05T14:02:13", note: "Hồ sơ hợp lệ, đang xử lý." },
  { id: "history-036", caseId: "case-013", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-04T08:05:33", note: "Hồ sơ được tiếp nhận trực tuyến." },
  { id: "history-037", caseId: "case-013", status: "Đang xử lý", createdAt: "2026-08-04T08:48:19", note: "Kiểm tra thông tin khai sinh." },
  { id: "history-038", caseId: "case-013", status: "Hoàn thành", createdAt: "2026-08-06T10:17:42", note: "Đã trả kết quả trực tuyến." },
  { id: "history-039", caseId: "case-014", status: "Tiếp nhận hồ sơ", createdAt: "2026-08-07T09:02:08", note: "Đã tiếp nhận hồ sơ môi trường." },
  { id: "history-040", caseId: "case-014", status: "Phân công chuyên viên", createdAt: "2026-08-07T09:21:36", note: "Phân công Vũ Thanh Hà xử lý." },
];

export const statusKeys = {
  "Mới tiếp nhận": "new",
  "Đang xử lý": "processing",
  "Sắp hạn": "upcoming",
  "Quá hạn": "overdue",
  "Hoàn thành": "completed",
};

export function getCaseRelations(caseItem) {
  const procedure = procedures.find((item) => item.id === caseItem.procedureId);
  const field = fields.find((item) => item.id === procedure?.fieldId);
  const user = users.find((item) => item.id === caseItem.assignedUserId);
  const department = departments.find((item) => item.id === user?.departmentId);

  return { procedure, field, user, department };
}
