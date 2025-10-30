CREATE TABLE IF NOT EXISTS formresponseanswers (
  Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  UserId BIGINT NOT NULL,
  FormKey INT NOT NULL,
  ResponseId BIGINT NOT NULL,
  FieldId VARCHAR(255) NOT NULL,
  FieldType ENUM ('shortText','textarea','email','number','date','radio','dropdown','checkbox','multiselect','mcq') NULL,
  AnswerValue LONGTEXT NULL,
  SubmittedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  KEY IX_FormResponseAnswers_ResponseId (ResponseId),
  KEY IX_formresponseanswers_UserId (UserId),
  KEY IX_FormResponseAnswers_FormKey (FormKey),
  CONSTRAINT FK_FormResponseAnswers_FormKeys_FormKey 
    FOREIGN KEY (FormKey) REFERENCES formkeys (FormKey) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;