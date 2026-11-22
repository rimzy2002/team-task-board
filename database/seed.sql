-- Optional data for a disposable schema. Run once, with no concurrent writers.
-- A schema that already contains application rows is left unchanged.
START TRANSACTION;

SET @seed_empty = NOT EXISTS (SELECT 1 FROM users)
              AND NOT EXISTS (SELECT 1 FROM projects)
              AND NOT EXISTS (SELECT 1 FROM tasks);
SET @seed_today = UTC_DATE();
SET @seed_now = UTC_TIMESTAMP(6);

INSERT INTO users (firebase_uid, email, created_at_utc)
SELECT demo.uid, NULL, @seed_now
FROM (
    SELECT 'fictional-seed-owner-a' AS uid UNION ALL
    SELECT 'fictional-seed-owner-b'
) AS demo
WHERE @seed_empty;

INSERT INTO projects (owner_id, name, description, created_at_utc, updated_at_utc)
SELECT u.id, demo.name, demo.description, @seed_now, @seed_now
FROM (
    SELECT 'fictional-seed-owner-a' AS uid, 'Campus open day' AS name,
           'Prepare the schedule and visitor materials.' AS description
    UNION ALL SELECT 'fictional-seed-owner-a', 'Volunteer onboarding', 'Organize the orientation package.'
    UNION ALL SELECT 'fictional-seed-owner-b', 'Library showcase', 'Prepare the branch display.'
) AS demo
INNER JOIN users AS u ON u.firebase_uid = demo.uid
WHERE @seed_empty;

INSERT INTO tasks (project_id, title, description, status, priority, due_date, assignee, created_at_utc, updated_at_utc)
SELECT p.id, demo.title, NULL, demo.status, demo.priority,
       DATE_ADD(@seed_today, INTERVAL demo.due_offset DAY), demo.assignee, @seed_now, @seed_now
FROM (
    SELECT 'Campus open day' AS project_name, 'Draft visitor handout' AS title,
           'ToDo' AS status, 'High' AS priority, -2 AS due_offset, 'Ada' AS assignee
    UNION ALL SELECT 'Campus open day', 'Check room capacity', 'InProgress', 'Medium', -1, NULL
    UNION ALL SELECT 'Campus open day', 'Publish advance notice', 'Done', 'Low', -3, 'Ada'
    UNION ALL SELECT 'Campus open day', 'Test signage layout', 'ToDo', 'Low', 0, 'Sam'
    UNION ALL SELECT 'Campus open day', 'Confirm event supplies', 'ToDo', 'Medium', 3, 'Sam'
    UNION ALL SELECT 'Volunteer onboarding', 'Review orientation slides', 'InProgress', 'High', -1, 'Ada'
    UNION ALL SELECT 'Volunteer onboarding', 'Collect badge preferences', 'ToDo', 'Low', NULL, NULL
    UNION ALL SELECT 'Library showcase', 'Check loan display', 'ToDo', 'High', -1, 'Riley'
    UNION ALL SELECT 'Library showcase', 'Package floor labels', 'Done', 'Medium', 2, 'Riley'
) AS demo
INNER JOIN projects AS p ON p.name = demo.project_name
WHERE @seed_empty;

COMMIT;
