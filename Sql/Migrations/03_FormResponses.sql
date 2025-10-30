CREATE TABLE IF NOT EXISTS formresponses (
  Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId BIGINT NOT NULL,
  FormKey INT NOT NULL,
  FormId VARCHAR(24) NOT NULL,
  SubmittedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY FK_FormResponses_Users_UserId (UserId),
  KEY IX_FormResponses_FormKey_SubmittedAt (FormKey, SubmittedAt),
  KEY IX_formresponses_FormId_UserId_SubmittedAt (FormId, UserId, SubmittedAt),
  CONSTRAINT FK_FormResponses_FormKeys_FormKey 
    FOREIGN KEY (FormKey) REFERENCES formkeys (FormKey) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;