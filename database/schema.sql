-- EF Core 10.0.12 forward script for 20261121143712_InitialSchema.
-- Select a new, empty utf8mb4 database on MySQL 8.4 before running this file.
-- Use either this script or the initial EF migration to create the tables.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `users` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `firebase_uid` varchar(128) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NOT NULL,
    `email` varchar(254) NULL,
    `created_at_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_users_firebase_uid` CHECK (CHAR_LENGTH(TRIM(firebase_uid)) > 0)
);

CREATE TABLE `projects` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `owner_id` bigint NOT NULL,
    `name` varchar(200) NOT NULL,
    `description` text NULL,
    `created_at_utc` datetime(6) NOT NULL,
    `updated_at_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_projects_name` CHECK (CHAR_LENGTH(TRIM(name)) > 0),
    CONSTRAINT `FK_projects_users_owner_id` FOREIGN KEY (`owner_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
);

CREATE TABLE `tasks` (
    `id` bigint NOT NULL AUTO_INCREMENT,
    `project_id` bigint NOT NULL,
    `title` varchar(200) NOT NULL,
    `description` text NULL,
    `status` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NOT NULL,
    `priority` varchar(8) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin NOT NULL,
    `due_date` date NULL,
    `assignee` varchar(200) NULL,
    `created_at_utc` datetime(6) NOT NULL,
    `updated_at_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`id`),
    CONSTRAINT `ck_tasks_priority` CHECK (priority IN ('Low', 'Medium', 'High')),
    CONSTRAINT `ck_tasks_status` CHECK (status IN ('ToDo', 'InProgress', 'Done')),
    CONSTRAINT `ck_tasks_title` CHECK (CHAR_LENGTH(TRIM(title)) > 0),
    CONSTRAINT `FK_tasks_projects_project_id` FOREIGN KEY (`project_id`) REFERENCES `projects` (`id`) ON DELETE RESTRICT
);

CREATE INDEX `ix_projects_owner_id` ON `projects` (`owner_id`);

CREATE INDEX `ix_tasks_project_status_priority` ON `tasks` (`project_id`, `status`, `priority`);

CREATE UNIQUE INDEX `ux_users_firebase_uid` ON `users` (`firebase_uid`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261121143712_InitialSchema', '10.0.12');

COMMIT;
