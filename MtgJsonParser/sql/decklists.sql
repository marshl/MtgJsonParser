CREATE TABLE decklists (
  id INT(11) NOT NULL AUTO_INCREMENT,
  ownerid INT(11) NOT NULL,
  deckname VARCHAR(50) NOT NULL,
  datecreated DATE DEFAULT NULL,
  datemodified DATE DEFAULT NULL,
  PRIMARY KEY (id)
)