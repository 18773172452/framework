SET NAMES utf8;
SET TIME_ZONE='+08:00';

CREATE TABLE IF NOT EXISTS `Security_Role` (
  `RoleId`      int unsigned NOT NULL COMMENT '主键，角色编号',
  `Namespace`   varchar(50)  NULL     COMMENT '命名空间，表示应用或组织机构的标识',
  `Module`      varchar(50)  NOT NULL DEFAULT '_' COMMENT '模块标识，表示对应的业务模块标识',
  `Name`        varchar(50)  NOT NULL COMMENT '角色名称，所属命名空间和模块内具有唯一性',
  `FullName`    varchar(50)  NULL     COMMENT '角色全称',
  `Description` varchar(500) NULL     COMMENT '描述信息',
  PRIMARY KEY (`RoleId`),
  UNIQUE INDEX `UX_Security_Role_Name` (`Namespace`, `Module`, `Name`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='角色表';

CREATE TABLE IF NOT EXISTS `Security_User` (
  `UserId`            int unsigned     NOT NULL COMMENT '主键，用户编号',
  `Namespace`         varchar(50)      NULL     COMMENT '命名空间，表示应用或组织机构的标识',
  `Name`              varchar(50)      NOT NULL COMMENT '用户名称，所属命名空间内具有唯一性',
  `FullName`          varchar(50)      NULL     COMMENT '用户全称',
  `Password`          varbinary(64)    NULL     COMMENT '用户的登录口令',
  `PasswordSalt`      bigint unsigned  NULL     COMMENT '口令加密向量(随机数)',
  `Email`             varchar(50)      NULL     COMMENT '用户的电子邮箱，该邮箱地址在所属命名空间内具有唯一性',
  `Phone`             varchar(50)      NULL     COMMENT '用户的手机号码，该手机号码在所属命名空间内具有唯一性',
  `Status`            tinyint unsigned NOT NULL COMMENT '用户状态（0:正常; 1:待批准; 2:已停用）',
  `StatusTimestamp`   datetime         NULL     COMMENT '状态更新时间',
  `PasswordQuestion1` varchar(50)      NULL     COMMENT '用户的密码问答的题面(1)',
  `PasswordAnswer1`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(1)',
  `PasswordQuestion2` varchar(50)      NULL     COMMENT '用户的密码问答的题面(2)',
  `PasswordAnswer2`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(2)',
  `PasswordQuestion3` varchar(50)      NULL     COMMENT '用户的密码问答的题面(3)',
  `PasswordAnswer3`   varbinary(64)    NULL     COMMENT '用户的密码问答的答案(3)',
  `Creation`          datetime         NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `Modification`      datetime         NULL     COMMENT '最后修改时间',
  `Description`       varchar(500)     NULL     COMMENT '描述信息',
  PRIMARY KEY (`UserId`),
  UNIQUE INDEX `UX_Security_User_Name` (`Namespace`, `Name`),
  UNIQUE INDEX `UX_Security_User_Email` (`Namespace`, `Email`),
  UNIQUE INDEX `UX_Security_User_Phone` (`Namespace`, `Phone`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='用户表';

CREATE TABLE IF NOT EXISTS `Security_Member` (
  `RoleId`     int unsigned     NOT NULL COMMENT '主键，角色编号',
  `MemberId`   int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType` tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  PRIMARY KEY (`RoleId`, `MemberId`, `MemberType`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='角色成员表';

CREATE TABLE IF NOT EXISTS `Security_Permission` (
  `MemberId`   int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType` tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  `SchemaId`   varchar(50)      NOT NULL COMMENT '主键，授权目标的标识',
  `ActionId`   varchar(50)      NOT NULL COMMENT '主键，授权行为的标识',
  `Granted`    tinyint(1)       NOT NULL COMMENT '是否授权(0: 表示拒绝; 1: 表示授予)',
  PRIMARY KEY (`MemberId`, `MemberType`, `SchemaId`, `ActionId`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='权限表';

CREATE TABLE IF NOT EXISTS `Security_PermissionFilter` (
  `MemberId`   int unsigned     NOT NULL COMMENT '主键，成员编号',
  `MemberType` tinyint unsigned NOT NULL COMMENT '主键，成员类型',
  `SchemaId`   varchar(50)      NOT NULL COMMENT '主键，授权目标的标识',
  `ActionId`   varchar(50)      NOT NULL COMMENT '主键，授权行为的标识',
  `Filter`     varchar(4000)    NOT NULL COMMENT '拒绝授权的过滤表达式',
  PRIMARY KEY (`MemberId`, `MemberType`, `SchemaId`, `ActionId`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='权限过滤表';

CREATE TABLE IF NOT EXISTS `Security_Censorship` (
  `Name` varchar(50) NOT NULL COMMENT '主键，审查类名',
  `Word` varchar(50) NOT NULL COMMENT '主键，阻止词汇',
  PRIMARY KEY (`Name`, `Word`)
) ENGINE = InnoDB DEFAULT CHARSET=utf8 COMMENT='词汇审查表';


/* 添加系统内置角色 */
INSERT INTO Security_Role (RoleId, Name, FullName, Description) VALUES (1, 'Administrators', '系统管理', '系统管理角色(系统内置角色)');
INSERT INTO Security_Role (RoleId, Name, FullName, Description) VALUES (2, 'Security', '安全管理', '安全管理角色(系统内置角色)');

/* 添加系统内置用户 */
INSERT INTO Security_User (UserId, Name, FullName, Description, Status) VALUES (1, 'Administrator', '系统管理员', '系统管理员(系统内置帐号)', 0);
INSERT INTO Security_User (UserId, Name, FullName, Description, Status) VALUES (2, 'Guest', '来宾', '来宾', 1);

/* 添加系统内置保留名字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Names', 'Zongsoft');

/* 添加非法关键字 */
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'fuck');
INSERT INTO Security_Censorship (Name, Word) VALUES ('Sensitives', 'bitch');
