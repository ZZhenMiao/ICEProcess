-- --------------------------------------------------------
-- 主机:                           localhost
-- 服务器版本:                        8.0.31 - MySQL Community Server - GPL
-- 服务器操作系统:                      Win64
-- HeidiSQL 版本:                  11.3.0.6295
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- 导出 iceprocess 的数据库结构
CREATE DATABASE IF NOT EXISTS `iceprocess` /*!40100 DEFAULT CHARACTER SET utf8mb3 */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `iceprocess`;

-- 导出  表 iceprocess.asset 结构
CREATE TABLE IF NOT EXISTS `asset` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(256) DEFAULT NULL COMMENT '名称（文件名）',
  `issequence` bit(1) DEFAULT b'1' COMMENT '是序列',
  `Illustration` varchar(1024) DEFAULT NULL COMMENT '说明（自定义关键字）',
  `path` varchar(1024) DEFAULT NULL COMMENT '名称',
  `id_assetdirectory` int unsigned DEFAULT NULL COMMENT '主目录ID',
  `archivetime` datetime DEFAULT CURRENT_TIMESTAMP COMMENT '归档日期',
  `id_uploader` int unsigned DEFAULT NULL COMMENT '上传者ID',
  PRIMARY KEY (`id`),
  KEY `FK_asset_assetdirectory` (`id_assetdirectory`),
  KEY `FK_asset_person` (`id_uploader`),
  CONSTRAINT `FK_asset_assetdirectory` FOREIGN KEY (`id_assetdirectory`) REFERENCES `assetdirectory` (`id`),
  CONSTRAINT `FK_asset_person` FOREIGN KEY (`id_uploader`) REFERENCES `person` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=483 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='资产';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.assetdirectory 结构
CREATE TABLE IF NOT EXISTS `assetdirectory` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `parentid` int unsigned DEFAULT NULL COMMENT '父目录ID',
  `id_nas` int unsigned DEFAULT NULL COMMENT 'NAS服务器的ID',
  PRIMARY KEY (`id`),
  KEY `FK_parent_assetdirectory` (`parentid`),
  KEY `FK_assetdirectory_nas` (`id_nas`),
  CONSTRAINT `FK_assetdirectory_nas` FOREIGN KEY (`id_nas`) REFERENCES `nas` (`id`),
  CONSTRAINT `FK_parent_assetdirectory` FOREIGN KEY (`parentid`) REFERENCES `assetdirectory` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=140 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='主目录树';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.assetlabel 结构
CREATE TABLE IF NOT EXISTS `assetlabel` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=480 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='资产标签';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.assetlabelgroup 结构
CREATE TABLE IF NOT EXISTS `assetlabelgroup` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` char(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `allassettype` bit(1) DEFAULT b'0' COMMENT '资产类型通用',
  `allproject` bit(1) DEFAULT b'1' COMMENT '项目通用',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=81 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='资产标签组';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.assettype 结构
CREATE TABLE IF NOT EXISTS `assettype` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` char(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `code` char(8) DEFAULT NULL COMMENT '资产类型代号',
  `parentid` int unsigned DEFAULT NULL COMMENT '父项ID',
  `enabled` bit(1) NOT NULL DEFAULT b'1' COMMENT '启用',
  PRIMARY KEY (`id`),
  KEY `FK_parent_assettype` (`parentid`),
  CONSTRAINT `FK_parent_assettype` FOREIGN KEY (`parentid`) REFERENCES `assettype` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1901 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='资产类型';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.assetusagelog 结构
CREATE TABLE IF NOT EXISTS `assetusagelog` (
  `id_asset` int unsigned NOT NULL,
  `usedata` datetime NOT NULL,
  `id_person` int unsigned NOT NULL,
  `usage` tinyint NOT NULL DEFAULT '1',
  KEY `FK_assetusagelog_asset` (`id_asset`),
  KEY `FK_assetusagelog_person` (`id_person`),
  CONSTRAINT `FK_assetusagelog_asset` FOREIGN KEY (`id_asset`) REFERENCES `asset` (`id`),
  CONSTRAINT `FK_assetusagelog_person` FOREIGN KEY (`id_person`) REFERENCES `person` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='资产使用记录表';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.asslib_extension 结构
CREATE TABLE IF NOT EXISTS `asslib_extension` (
  `name` varchar(16) DEFAULT NULL,
  `illustration` varchar(1024) DEFAULT NULL,
  `path` varchar(512) DEFAULT NULL,
  `args` varchar(1024) DEFAULT NULL,
  `allobligations` bit(1) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3;

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assetlabelgroup_assetlabel 结构
CREATE TABLE IF NOT EXISTS `ass_assetlabelgroup_assetlabel` (
  `id_assetlabelgroup` int unsigned NOT NULL COMMENT '资产标签组的ID',
  `id_assetlabel` int unsigned NOT NULL COMMENT '资产标签的ID',
  KEY `FK_ass_assetlabel_assetlabelgroup` (`id_assetlabelgroup`),
  KEY `FK_ass_assetlabelgroup_assetlabel` (`id_assetlabel`),
  CONSTRAINT `FK_ass_assetlabel_assetlabelgroup` FOREIGN KEY (`id_assetlabelgroup`) REFERENCES `assetlabelgroup` (`id`),
  CONSTRAINT `FK_ass_assetlabelgroup_assetlabel` FOREIGN KEY (`id_assetlabel`) REFERENCES `assetlabel` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产标签组-资产标签（标签与标签组的下级分类对应关系）';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assetlabel_assetlabelgroup 结构
CREATE TABLE IF NOT EXISTS `ass_assetlabel_assetlabelgroup` (
  `id_assetlabel` int unsigned NOT NULL,
  `id_assetlabelgroup` int unsigned NOT NULL,
  KEY `ass_assetlabel_assetlabelgroup_assetlabel` (`id_assetlabel`),
  KEY `ass_assetlabel_assetlabelgroup_assetlabelgroup` (`id_assetlabelgroup`),
  CONSTRAINT `ass_assetlabel_assetlabelgroup_assetlabel` FOREIGN KEY (`id_assetlabel`) REFERENCES `assetlabel` (`id`),
  CONSTRAINT `ass_assetlabel_assetlabelgroup_assetlabelgroup` FOREIGN KEY (`id_assetlabelgroup`) REFERENCES `assetlabelgroup` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb3 COMMENT='关联表：资产标签-资产标签组（标签与标签组的上级对应关系，既基本父子关系）';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assettype_asset 结构
CREATE TABLE IF NOT EXISTS `ass_assettype_asset` (
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  `id_asset` int unsigned NOT NULL COMMENT '资产的ID',
  KEY `FK_asset_assettype` (`id_asset`),
  KEY `FK_assettype_asset` (`id_assettype`),
  CONSTRAINT `FK_asset_assettype` FOREIGN KEY (`id_asset`) REFERENCES `asset` (`id`),
  CONSTRAINT `FK_assettype_asset` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产类型-资产';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assettype_assetlabel 结构
CREATE TABLE IF NOT EXISTS `ass_assettype_assetlabel` (
  `id_assetlabel` int unsigned NOT NULL COMMENT '标签的ID',
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  KEY `FK_assetlabel_assettype` (`id_assetlabel`),
  KEY `FK_assettype_assetlabel` (`id_assettype`),
  CONSTRAINT `FK_assetlabel_assettype` FOREIGN KEY (`id_assetlabel`) REFERENCES `assetlabel` (`id`),
  CONSTRAINT `FK_assettype_assetlabel` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产类型-标签';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assettype_assetlabelgroup 结构
CREATE TABLE IF NOT EXISTS `ass_assettype_assetlabelgroup` (
  `id_assetlabelgroup` int unsigned NOT NULL COMMENT '标签组的ID',
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  KEY `FK_assetlabelgroup_assettype` (`id_assetlabelgroup`),
  KEY `FK_assettype_assetlabelgroup` (`id_assettype`),
  CONSTRAINT `FK_assetlabelgroup_assettype` FOREIGN KEY (`id_assetlabelgroup`) REFERENCES `assetlabelgroup` (`id`),
  CONSTRAINT `FK_assettype_assetlabelgroup` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产类型-标签组';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_assettype_obligation 结构
CREATE TABLE IF NOT EXISTS `ass_assettype_obligation` (
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  `id_obligation` int unsigned NOT NULL COMMENT '职能的ID',
  `relationship` tinyint NOT NULL DEFAULT '0' COMMENT '关联关系，-1：无权限;0：可查询;1：可编辑标签；2：可编辑标签组。',
  KEY `FK_obligation_assettype` (`id_assettype`),
  KEY `FK_assettype_obligation` (`id_obligation`),
  CONSTRAINT `FK_assettype_obligation` FOREIGN KEY (`id_obligation`) REFERENCES `obligation` (`id`),
  CONSTRAINT `FK_obligation_assettype` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产类型-职能';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_asset_assetlabel 结构
CREATE TABLE IF NOT EXISTS `ass_asset_assetlabel` (
  `id_asset` int unsigned NOT NULL COMMENT '资产的ID',
  `id_assetlabel` int unsigned NOT NULL COMMENT '标签的ID',
  KEY `FK_asset_assetlabel` (`id_asset`),
  KEY `FK_assetlabel_asset` (`id_assetlabel`),
  CONSTRAINT `FK_asset_assetlabel` FOREIGN KEY (`id_asset`) REFERENCES `asset` (`id`),
  CONSTRAINT `FK_assetlabel_asset` FOREIGN KEY (`id_assetlabel`) REFERENCES `assetlabel` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：资产-标签';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_autoarchivescheme_assetlabel 结构
CREATE TABLE IF NOT EXISTS `ass_autoarchivescheme_assetlabel` (
  `id_autoarchivescheme` int unsigned NOT NULL COMMENT '自动归档方案的ID',
  `id_assetlabel` int unsigned NOT NULL COMMENT '标签的ID',
  KEY `FK_autoarchivescheme_assetlabel` (`id_autoarchivescheme`),
  KEY `FK_assetlabel_autoarchivescheme` (`id_assetlabel`),
  CONSTRAINT `FK_assetlabel_autoarchivescheme` FOREIGN KEY (`id_assetlabel`) REFERENCES `assetlabel` (`id`),
  CONSTRAINT `FK_autoarchivescheme_assetlabel` FOREIGN KEY (`id_autoarchivescheme`) REFERENCES `autoarchivescheme` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：自动归档方案-标签';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_autoarchivescheme_assettype 结构
CREATE TABLE IF NOT EXISTS `ass_autoarchivescheme_assettype` (
  `id_autoarchivescheme` int unsigned NOT NULL COMMENT '自动归档方案的ID',
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  KEY `FK_autoarchivescheme_assettype` (`id_autoarchivescheme`),
  KEY `FK_assettype_autoarchivescheme` (`id_assettype`),
  CONSTRAINT `FK_assettype_autoarchivescheme` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`),
  CONSTRAINT `FK_autoarchivescheme_assettype` FOREIGN KEY (`id_autoarchivescheme`) REFERENCES `autoarchivescheme` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：自动归档方案-资产类型';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_autoarchivescheme_project 结构
CREATE TABLE IF NOT EXISTS `ass_autoarchivescheme_project` (
  `id_autoarchivescheme` int unsigned NOT NULL COMMENT '自动归档方案的ID',
  `id_project` int unsigned NOT NULL COMMENT '项目的ID',
  KEY `FK_autoarchivescheme_project` (`id_autoarchivescheme`),
  KEY `FK_project_autoarchivescheme` (`id_project`),
  CONSTRAINT `FK_autoarchivescheme_project` FOREIGN KEY (`id_autoarchivescheme`) REFERENCES `autoarchivescheme` (`id`),
  CONSTRAINT `FK_project_autoarchivescheme` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：自动归档方案-项目';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_module_assettype 结构
CREATE TABLE IF NOT EXISTS `ass_module_assettype` (
  `id_module` int unsigned NOT NULL COMMENT '模块的ID',
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  KEY `FK_module_assettype` (`id_module`),
  KEY `FK_assettype_module` (`id_assettype`),
  CONSTRAINT `FK_assettype_module` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`),
  CONSTRAINT `FK_module_assettype` FOREIGN KEY (`id_module`) REFERENCES `module` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：模块-资产类型';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_module_module 结构
CREATE TABLE IF NOT EXISTS `ass_module_module` (
  `id_process` int unsigned NOT NULL COMMENT '流程的ID',
  `id_module_a` int unsigned NOT NULL COMMENT '模块A的ID',
  `id_module_b` int unsigned NOT NULL COMMENT '模块B的ID',
  `relationship` tinyint unsigned NOT NULL DEFAULT '0' COMMENT '关联关系，0代表B引用A的内容，1代表A是B的父模块。',
  KEY `id_process` (`id_process`),
  KEY `FK_module_a` (`id_module_a`),
  KEY `FK_module_b` (`id_module_b`),
  CONSTRAINT `FK_module_a` FOREIGN KEY (`id_module_a`) REFERENCES `module` (`id`),
  CONSTRAINT `FK_module_b` FOREIGN KEY (`id_module_b`) REFERENCES `module` (`id`),
  CONSTRAINT `id_process` FOREIGN KEY (`id_process`) REFERENCES `process` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：模块-模块';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_module_task 结构
CREATE TABLE IF NOT EXISTS `ass_module_task` (
  `id_module` int unsigned NOT NULL COMMENT '模块的ID',
  `id_task` int unsigned NOT NULL COMMENT '任务的ID',
  KEY `FK_task_module` (`id_module`),
  KEY `FK_module_task` (`id_task`),
  CONSTRAINT `FK_module_task` FOREIGN KEY (`id_task`) REFERENCES `task` (`id`),
  CONSTRAINT `FK_task_module` FOREIGN KEY (`id_module`) REFERENCES `module` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：模块-任务';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_position_obligation 结构
CREATE TABLE IF NOT EXISTS `ass_position_obligation` (
  `id_position` int unsigned NOT NULL COMMENT '职位的ID',
  `id_obligation` int unsigned NOT NULL COMMENT '职能的ID',
  KEY `FK_obligation_position` (`id_position`),
  KEY `FK_position_obligation` (`id_obligation`),
  CONSTRAINT `FK_obligation_position` FOREIGN KEY (`id_position`) REFERENCES `position` (`id`),
  CONSTRAINT `FK_position_obligation` FOREIGN KEY (`id_obligation`) REFERENCES `obligation` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：职位-职能';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_position_person 结构
CREATE TABLE IF NOT EXISTS `ass_position_person` (
  `id_position` int unsigned NOT NULL COMMENT '职位的ID',
  `id_person` int unsigned NOT NULL COMMENT '人员的ID',
  KEY `FK_person_position` (`id_position`),
  KEY `FK_position_person` (`id_person`),
  CONSTRAINT `FK_person_position` FOREIGN KEY (`id_position`) REFERENCES `position` (`id`),
  CONSTRAINT `FK_position_person` FOREIGN KEY (`id_person`) REFERENCES `person` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：职位-人员';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_process_task 结构
CREATE TABLE IF NOT EXISTS `ass_process_task` (
  `id_process` int unsigned NOT NULL COMMENT '流程的ID',
  `id_task` int unsigned NOT NULL COMMENT '任务的ID',
  KEY `FK_process_task` (`id_process`),
  KEY `FK_task_process` (`id_task`),
  CONSTRAINT `FK_process_task` FOREIGN KEY (`id_process`) REFERENCES `process` (`id`),
  CONSTRAINT `FK_task_process` FOREIGN KEY (`id_task`) REFERENCES `task` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：流程-任务';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_project_asset 结构
CREATE TABLE IF NOT EXISTS `ass_project_asset` (
  `id_asset` int unsigned NOT NULL COMMENT '资产的ID',
  `id_project` int unsigned NOT NULL COMMENT '项目的ID',
  KEY `FK_asset_project` (`id_asset`),
  KEY `FK_project_asset` (`id_project`),
  CONSTRAINT `FK_asset_project` FOREIGN KEY (`id_asset`) REFERENCES `asset` (`id`),
  CONSTRAINT `FK_project_asset` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：项目-资产';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_project_assetlabelgroup 结构
CREATE TABLE IF NOT EXISTS `ass_project_assetlabelgroup` (
  `id_assetlabelgroup` int unsigned NOT NULL COMMENT '标签组的ID',
  `id_project` int unsigned NOT NULL COMMENT '项目的ID',
  KEY `FK_assetlabelgroup_project` (`id_assetlabelgroup`),
  KEY `FK_project_assetlabelgroup` (`id_project`),
  CONSTRAINT `FK_assetlabelgroup_project` FOREIGN KEY (`id_assetlabelgroup`) REFERENCES `assetlabelgroup` (`id`),
  CONSTRAINT `FK_project_assetlabelgroup` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：项目-标签组';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.ass_task_assettype 结构
CREATE TABLE IF NOT EXISTS `ass_task_assettype` (
  `id_task` int unsigned NOT NULL COMMENT '任务的ID',
  `id_assettype` int unsigned NOT NULL COMMENT '资产类型的ID',
  `relationship` tinyint unsigned NOT NULL DEFAULT '0' COMMENT '关联关系，0代表作为产出，1代表作为原料。',
  KEY `FK_assettype_task` (`id_assettype`),
  KEY `FK_task_assettype` (`id_task`),
  CONSTRAINT `FK_assettype_task` FOREIGN KEY (`id_assettype`) REFERENCES `assettype` (`id`),
  CONSTRAINT `FK_task_assettype` FOREIGN KEY (`id_task`) REFERENCES `task` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='关联表：任务-资产类型';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.autoarchivescheme 结构
CREATE TABLE IF NOT EXISTS `autoarchivescheme` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `id_assetdirectory` int unsigned NOT NULL COMMENT '资产主目录ID',
  PRIMARY KEY (`id`),
  KEY `FK_autoarchivescheme_assetdirectory` (`id_assetdirectory`),
  CONSTRAINT `FK_autoarchivescheme_assetdirectory` FOREIGN KEY (`id_assetdirectory`) REFERENCES `assetdirectory` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=76 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='自动归档方案';

-- 数据导出被取消选择。

-- 导出  存储过程 iceprocess.func_matchassetdirectory 结构
DELIMITER //
CREATE PROCEDURE `func_matchassetdirectory`(
	IN `t` varchar(512),
	IN `p` varchar(512),
	IN `l` varchar(512)
)
begin
	set @stmt = concat('SELECT * FROM assetdirectory WHERE id IN (SELECT id_assetdirectory FROM autoarchivescheme WHERE id IN (select t2.* from (SELECT ass_autoarchivescheme_assettype.id_autoarchivescheme FROM (ass_autoarchivescheme_assettype LEFT JOIN ass_autoarchivescheme_project ON ass_autoarchivescheme_project.id_autoarchivescheme IS NULL OR ass_autoarchivescheme_project.id_autoarchivescheme = ass_autoarchivescheme_assettype.id_autoarchivescheme) LEFT JOIN ass_autoarchivescheme_assetlabel ON ass_autoarchivescheme_assetlabel.id_autoarchivescheme IS NULL OR ass_autoarchivescheme_assetlabel.id_autoarchivescheme = ass_autoarchivescheme_assettype.id_autoarchivescheme WHERE id_assettype ',t,' AND id_project ',p,' AND (id_assetlabel ',l,' OR id_assetlabel is null) GROUP BY id_autoarchivescheme ORDER BY COUNT(*) DESC LIMIT 3) as t2));');
	prepare stmt from @stmt;
    execute stmt;
    deallocate prepare stmt;
end//
DELIMITER ;

-- 导出  表 iceprocess.module 结构
CREATE TABLE IF NOT EXISTS `module` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `Identificationcode` varchar(64) DEFAULT NULL COMMENT '识别码',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='模块';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.nas 结构
CREATE TABLE IF NOT EXISTS `nas` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `ip` char(15) DEFAULT NULL COMMENT 'IP地址',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='NAS服务器';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.obligation 结构
CREATE TABLE IF NOT EXISTS `obligation` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `allassettype` tinyint NOT NULL DEFAULT '0' COMMENT '与所有资产类型的关联关系，低优先级，-1：无权限;0：可查询;1：可编辑标签；2：可编辑标签组。',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='职能';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.person 结构
CREATE TABLE IF NOT EXISTS `person` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '姓名',
  `account` varchar(32) DEFAULT NULL COMMENT '账号名',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `password` char(8) DEFAULT NULL COMMENT '密码',
  `gender` bit(1) DEFAULT b'1' COMMENT '性别',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=33 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='人员';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.position 结构
CREATE TABLE IF NOT EXISTS `position` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `id_team` int unsigned NOT NULL COMMENT 'ID',
  PRIMARY KEY (`id`),
  KEY `FK_position_team` (`id_team`),
  CONSTRAINT `FK_position_team` FOREIGN KEY (`id_team`) REFERENCES `team` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='职位';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.process 结构
CREATE TABLE IF NOT EXISTS `process` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='流程';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.production 结构
CREATE TABLE IF NOT EXISTS `production` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `code` varchar(32) DEFAULT NULL COMMENT '代号',
  `name` varchar(32) DEFAULT NULL COMMENT '名称',
  `illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='产品';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.project 结构
CREATE TABLE IF NOT EXISTS `project` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `id_module` int unsigned DEFAULT NULL COMMENT '所属模块ID',
  `id_project` int unsigned DEFAULT NULL COMMENT '父项目ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`id`),
  KEY `FK_project_module` (`id_module`),
  KEY `FK_project_project` (`id_project`),
  CONSTRAINT `FK_project_module` FOREIGN KEY (`id_module`) REFERENCES `module` (`id`),
  CONSTRAINT `FK_project_project` FOREIGN KEY (`id_project`) REFERENCES `project` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='项目';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.publicsetting 结构
CREATE TABLE IF NOT EXISTS `publicsetting` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(32) DEFAULT NULL COMMENT '名称',
  `velue` varchar(512) DEFAULT NULL COMMENT '说明',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='设置';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.task 结构
CREATE TABLE IF NOT EXISTS `task` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `parentid` int unsigned DEFAULT NULL COMMENT '父项ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `code` varchar(8) DEFAULT NULL COMMENT '代号',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=24 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='任务';

-- 数据导出被取消选择。

-- 导出  表 iceprocess.team 结构
CREATE TABLE IF NOT EXISTS `team` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'ID',
  `name` varchar(16) DEFAULT NULL COMMENT '名称',
  `Illustration` varchar(64) DEFAULT NULL COMMENT '说明',
  `parentid` int unsigned DEFAULT NULL COMMENT '父项ID',
  PRIMARY KEY (`id`),
  KEY `FK_parent_team` (`parentid`),
  CONSTRAINT `FK_parent_team` FOREIGN KEY (`parentid`) REFERENCES `team` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='团队';

-- 数据导出被取消选择。

-- 导出  存储过程 iceprocess.test01 结构
DELIMITER //
CREATE PROCEDURE `test01`(in t1 varchar(16),in t2 varchar(16))
begin
	set @stmt = concat('SELECT ',t2,'.id AS id, ',t1,'.name AS n1, ',t2,'.name AS n2 FROM ',t1,' RIGHT JOIN ',t2,' on ',t1,'.id=',t2,'.id');
	prepare stmt from @stmt;
    execute stmt;
    deallocate prepare stmt;
end//
DELIMITER ;

-- 导出  存储过程 iceprocess.test02 结构
DELIMITER //
CREATE PROCEDURE `test02`(in t1 varchar(16),in t2 varchar(16),in t1n varchar(16))
begin
	set @stmt = concat('select * from ',t2,' where id in (select id_',t2,' from ass_',t1,'_',t2,' where id_',t1,'=(select id from ',t1,' where name=\'',t1n,'\'))');
	prepare stmt from @stmt;
    execute stmt;
    deallocate prepare stmt;
end//
DELIMITER ;

/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
