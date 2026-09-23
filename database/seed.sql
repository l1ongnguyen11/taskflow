-- ============================================================================
-- TaskFlow — PostgreSQL Seed Data Script
-- Version: 1.0
-- Description: Seeds 2 test accounts (Team Lead & Member) and comprehensive
--              mock data across Workspaces, Projects, Kanban Boards, Columns,
--              Tasks (To Do, In Progress, Code Review, Done), Sprints, Comments,
--              Time Logs, and Checklists.
--- ============================================================================

-- Password for both test accounts: Password123!
-- BCrypt Hash: $2a$11$w8c6T6/tq7Y9Q3u4pA0kDeKkFjS/gLqP.j5xQ9B1E4H7I0J3K6L5M

DO $$
DECLARE
    v_lead_id        UUID := '11111111-1111-1111-1111-111111111111';
    v_member_id      UUID := '22222222-2222-2222-2222-222222222222';
    v_workspace_id   UUID := '33333333-3333-3333-3333-333333333333';
    
    v_owner_role_id  UUID;
    v_member_role_id UUID;
    
    v_proj_csa_id    UUID := '44444444-4444-4444-4444-444444444441';
    v_board_csa_id   UUID := '55555555-5555-5555-5555-555555555551';
    
    v_col_todo_id    UUID := '66666666-6666-6666-6666-666666666661';
    v_col_inprog_id  UUID := '66666666-6666-6666-6666-666666666662';
    v_col_review_id  UUID := '66666666-6666-6666-6666-666666666663';
    v_col_done_id    UUID := '66666666-6666-6666-6666-666666666664';
    
    v_sprint_id      UUID := '77777777-7777-7777-7777-777777777771';
    
    v_lbl_backend_id UUID := '88888888-8888-8888-8888-888888888881';
    v_lbl_front_id   UUID := '88888888-8888-8888-8888-888888888882';
    v_lbl_db_id      UUID := '88888888-8888-8888-8888-888888888883';
    v_lbl_sec_id     UUID := '88888888-8888-8888-8888-888888888884';

    v_task1_id UUID := gen_random_uuid();
    v_task2_id UUID := gen_random_uuid();
    v_task3_id UUID := gen_random_uuid();
    v_task4_id UUID := gen_random_uuid();
    v_task5_id UUID := gen_random_uuid();
    v_task6_id UUID := gen_random_uuid();
    v_task7_id UUID := gen_random_uuid();
    v_task8_id UUID := gen_random_uuid();
    v_task9_id UUID := gen_random_uuid();

    v_pwd_hash TEXT := '$2a$11$w8c6T6/tq7Y9Q3u4pA0kDeKkFjS/gLqP.j5xQ9B1E4H7I0J3K6L5M';
BEGIN

    -- 1. Create Test Users
    INSERT INTO "user" (id, email, password_hash, display_name, is_active, created_at, updated_at)
    VALUES 
        (v_lead_id,   'lead@taskflow.dev',   v_pwd_hash, 'Nguyen Van Lead (Trưởng Nhóm)', true, NOW(), NOW()),
        (v_member_id, 'member@taskflow.dev', v_pwd_hash, 'Tran Thi Member (Thành Viên)', true, NOW(), NOW())
    ON CONFLICT (email) DO NOTHING;

    -- Get Role IDs
    SELECT id INTO v_owner_role_id FROM role WHERE name = 'owner';
    SELECT id INTO v_member_role_id FROM role WHERE name = 'member';

    -- 2. Create Workspace
    INSERT INTO workspace (id, name, slug, description, created_by, created_at, updated_at)
    VALUES (
        v_workspace_id, 
        'TaskFlow Global Core', 
        'taskflow-global-core', 
        'Workspace chính dành cho dự án demo TaskFlow với đầy đủ các dự án, bảng Kanban và tác vụ mẫu.',
        v_lead_id, 
        NOW(), NOW()
    ) ON CONFLICT (slug) DO NOTHING;

    -- 3. Add Workspace Members & Roles
    INSERT INTO workspace_member (id, workspace_id, user_id, joined_at, created_at, updated_at)
    VALUES 
        (gen_random_uuid(), v_workspace_id, v_lead_id, NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_workspace_id, v_member_id, NOW(), NOW(), NOW())
    ON CONFLICT (workspace_id, user_id) DO NOTHING;

    IF v_owner_role_id IS NOT NULL THEN
        INSERT INTO user_role (id, user_id, role_id, workspace_id, created_at, updated_at)
        VALUES (gen_random_uuid(), v_lead_id, v_owner_role_id, v_workspace_id, NOW(), NOW())
        ON CONFLICT (user_id, role_id, workspace_id) DO NOTHING;
    END IF;

    IF v_member_role_id IS NOT NULL THEN
        INSERT INTO user_role (id, user_id, role_id, workspace_id, created_at, updated_at)
        VALUES (gen_random_uuid(), v_member_id, v_member_role_id, v_workspace_id, NOW(), NOW())
        ON CONFLICT (user_id, role_id, workspace_id) DO NOTHING;
    END IF;

    -- 4. Create Workspace Labels
    INSERT INTO label (id, workspace_id, name, color, created_at, updated_at)
    VALUES 
        (v_lbl_backend_id, v_workspace_id, 'Backend',  '#3B82F6', NOW(), NOW()),
        (v_lbl_front_id,   v_workspace_id, 'Frontend', '#10B981', NOW(), NOW()),
        (v_lbl_db_id,      v_workspace_id, 'Database', '#F59E0B', NOW(), NOW()),
        (v_lbl_sec_id,     v_workspace_id, 'Security', '#EF4444', NOW(), NOW())
    ON CONFLICT DO NOTHING;

    -- 5. Create Project: Core System Architecture
    INSERT INTO project (id, workspace_id, name, key, description, lead_id, is_archived, created_at, updated_at)
    VALUES (
        v_proj_csa_id, 
        v_workspace_id, 
        'Core System Architecture', 
        'CSA', 
        'Dự án hạ tầng hệ thống backend .NET 8, cơ sở dữ liệu PostgreSQL và kiến trúc Clean Architecture.',
        v_lead_id, 
        false, 
        NOW(), NOW()
    ) ON CONFLICT (workspace_id, key) DO NOTHING;

    INSERT INTO project_member (id, project_id, user_id, role, created_at, updated_at)
    VALUES 
        (gen_random_uuid(), v_proj_csa_id, v_lead_id, 'lead', NOW(), NOW()),
        (gen_random_uuid(), v_proj_csa_id, v_member_id, 'member', NOW(), NOW())
    ON CONFLICT DO NOTHING;

    -- 6. Create Board & Columns
    INSERT INTO board (id, project_id, name, description, created_at, updated_at)
    VALUES (v_board_csa_id, v_proj_csa_id, 'CSA Sprint Board', 'Bản đồ theo dõi tiến độ sprint dự án CSA', NOW(), NOW())
    ON CONFLICT DO NOTHING;

    INSERT INTO board_column (id, board_id, name, position, color, wip_limit, is_done_column, created_at, updated_at)
    VALUES 
        (v_col_todo_id,   v_board_csa_id, 'To Do',       0, '#6B7280', NULL, false, NOW(), NOW()),
        (v_col_inprog_id, v_board_csa_id, 'In Progress', 1, '#3B82F6', 5,    false, NOW(), NOW()),
        (v_col_review_id, v_board_csa_id, 'Code Review', 2, '#F59E0B', 3,    false, NOW(), NOW()),
        (v_col_done_id,   v_board_csa_id, 'Done',        3, '#10B981', NULL, true,  NOW(), NOW())
    ON CONFLICT DO NOTHING;

    -- 7. Active Sprint
    INSERT INTO sprint (id, project_id, name, goal, start_date, end_date, status, created_at, updated_at)
    VALUES (
        v_sprint_id, v_proj_csa_id, 'Sprint 1 - Core & Auth Integration',
        'Hoàn thiện hạ tầng xác thực JWT, Refresh Token và giao diện Kanban Board core.',
        CURRENT_DATE - INTERVAL '5 days', CURRENT_DATE + INTERVAL '9 days', 'active', NOW(), NOW()
    ) ON CONFLICT DO NOTHING;

    -- 8. Create Tasks in all columns
    -- TO DO Tasks
    INSERT INTO task (id, board_column_id, sprint_id, reporter_id, title, description, type, priority, task_number, position, story_points, start_date, due_date, created_at, updated_at)
    VALUES 
    (v_task1_id, v_col_todo_id, v_sprint_id, v_lead_id, 'Thiết kế RESTful API cho Module Notification', 'Định nghĩa DTO và endpoint cho việc truy vấn danh sách thông báo và đánh dấu đã đọc.', 'story', 'high', 1, 0, 5, CURRENT_DATE, CURRENT_DATE + INTERVAL '3 days', NOW(), NOW()),
    (v_task2_id, v_col_todo_id, v_sprint_id, v_lead_id, 'Tối ưu hóa query PostgreSQL cho Dashboard KPI', 'Viết query tổng hợp số lượng công việc theo trạng thái và độ ưu tiên mà không gây nghẽn DB.', 'task', 'medium', 2, 1, 3, CURRENT_DATE, CURRENT_DATE + INTERVAL '4 days', NOW(), NOW());

    -- IN PROGRESS Tasks
    INSERT INTO task (id, board_column_id, sprint_id, reporter_id, title, description, type, priority, task_number, position, story_points, start_date, due_date, created_at, updated_at)
    VALUES 
    (v_task3_id, v_col_inprog_id, v_sprint_id, v_lead_id, 'Tích hợp JWT Refresh Token rotation trên Frontend React', 'Cấu hình Axios Interceptor tự động gửi request /refresh-token khi gặp lỗi 401 Unauthorized.', 'task', 'highest', 3, 0, 8, CURRENT_DATE - INTERVAL '2 days', CURRENT_DATE + INTERVAL '2 days', NOW(), NOW()),
    (v_task4_id, v_col_inprog_id, v_sprint_id, v_member_id, 'Xây dựng giao diện Drag-and-Drop cho Kanban Board', 'Sử dụng thư viện @dnd-kit cho phép kéo thả thẻ giữa To Do, In Progress, Review, Done.', 'story', 'high', 4, 1, 5, CURRENT_DATE - INTERVAL '3 days', CURRENT_DATE + INTERVAL '1 day', NOW(), NOW());

    -- CODE REVIEW Tasks
    INSERT INTO task (id, board_column_id, sprint_id, reporter_id, title, description, type, priority, task_number, position, story_points, start_date, due_date, created_at, updated_at)
    VALUES 
    (v_task5_id, v_col_review_id, v_sprint_id, v_lead_id, 'Cấu hình Docker Compose cho PostgreSQL & Backend .NET 8', 'Dockerfile và docker-compose.yml khởi chạy nhanh PostgreSQL database và Web API.', 'task', 'high', 5, 0, 3, CURRENT_DATE - INTERVAL '4 days', CURRENT_DATE - INTERVAL '1 day', NOW(), NOW()),
    (v_task6_id, v_col_review_id, v_sprint_id, v_member_id, 'Thêm validation giới hạn dung lượng file đính kèm 10MB', 'Bổ sung FluentValidation từ chối các file lớn hơn 10MB hoặc sai định dạng.', 'bug', 'medium', 6, 1, 2, CURRENT_DATE - INTERVAL '3 days', CURRENT_DATE, NOW(), NOW());

    -- DONE Tasks
    INSERT INTO task (id, board_column_id, sprint_id, reporter_id, title, description, type, priority, task_number, position, story_points, start_date, due_date, completed_at, created_at, updated_at)
    VALUES 
    (v_task7_id, v_col_done_id, v_sprint_id, v_lead_id, 'Khởi tạo Clean Architecture Solution cho .NET 8', 'Phân chia đầy đủ 4 tầng API, Application, Domain, Infrastructure theo Clean Architecture.', 'epic', 'highest', 7, 0, 13, CURRENT_DATE - INTERVAL '7 days', CURRENT_DATE - INTERVAL '5 days', NOW() - INTERVAL '5 days', NOW(), NOW()),
    (v_task8_id, v_col_done_id, v_sprint_id, v_lead_id, 'Thiết lập cơ sở dữ liệu PostgreSQL schema & Enum types', 'Tạo 28 bảng dữ liệu chuẩn hóa 3NF và thiết lập các trigger auto-update TIMESTAMPTZ.', 'task', 'high', 8, 1, 8, CURRENT_DATE - INTERVAL '6 days', CURRENT_DATE - INTERVAL '4 days', NOW() - INTERVAL '4 days', NOW(), NOW()),
    (v_task9_id, v_col_done_id, v_sprint_id, v_member_id, 'Tạo trang Đăng nhập & Đăng ký với i18n đa ngôn ngữ', 'Giao diện React hiện đại hỗ trợ tiếng Việt và tiếng Anh linh hoạt.', 'story', 'high', 9, 2, 5, CURRENT_DATE - INTERVAL '5 days', CURRENT_DATE - INTERVAL '3 days', NOW() - INTERVAL '3 days', NOW(), NOW());

    -- 9. Assignees & Labels
    INSERT INTO task_assignee (id, task_id, user_id, assigned_at, created_at, updated_at) VALUES
        (gen_random_uuid(), v_task1_id, v_member_id, NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task2_id, v_lead_id,   NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task3_id, v_member_id, NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task4_id, v_lead_id,   NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task5_id, v_lead_id,   NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task6_id, v_member_id, NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task7_id, v_lead_id,   NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task8_id, v_lead_id,   NOW(), NOW(), NOW()),
        (gen_random_uuid(), v_task9_id, v_member_id, NOW(), NOW(), NOW())
    ON CONFLICT DO NOTHING;

    INSERT INTO task_label (id, task_id, label_id, created_at, updated_at) VALUES
        (gen_random_uuid(), v_task1_id, v_lbl_backend_id, NOW(), NOW()),
        (gen_random_uuid(), v_task2_id, v_lbl_db_id,      NOW(), NOW()),
        (gen_random_uuid(), v_task3_id, v_lbl_sec_id,     NOW(), NOW()),
        (gen_random_uuid(), v_task4_id, v_lbl_front_id,   NOW(), NOW()),
        (gen_random_uuid(), v_task5_id, v_lbl_backend_id, NOW(), NOW()),
        (gen_random_uuid(), v_task6_id, v_lbl_backend_id, NOW(), NOW()),
        (gen_random_uuid(), v_task7_id, v_lbl_backend_id, NOW(), NOW()),
        (gen_random_uuid(), v_task8_id, v_lbl_db_id,      NOW(), NOW()),
        (gen_random_uuid(), v_task9_id, v_lbl_front_id,   NOW(), NOW())
    ON CONFLICT DO NOTHING;

    -- 10. Comments
    INSERT INTO comment (id, task_id, user_id, body, created_at, updated_at) VALUES
        (gen_random_uuid(), v_task4_id, v_lead_id,   'Tôi đã hoàn thành component KanbanColumn với @dnd-kit, nhờ bạn test giúp trên mobile nhé.', NOW() - INTERVAL '12 hours', NOW()),
        (gen_random_uuid(), v_task4_id, v_member_id, 'Đã test thử trên Safari và Chrome Mobile, giao diện tương tác rất mượt mà!', NOW() - INTERVAL '6 hours', NOW()),
        (gen_random_uuid(), v_task5_id, v_member_id, 'Em đã xem PR Docker Compose, các container Postgres và API khởi chạy đúng thứ tự. Approved!', NOW() - INTERVAL '3 hours', NOW())
    ON CONFLICT DO NOTHING;

END $$;
