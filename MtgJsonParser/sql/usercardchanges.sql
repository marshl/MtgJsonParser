CREATE TABLE usercardchanges (
  id INT(11) NOT NULL AUTO_INCREMENT,
  userid INT(11) NOT NULL,
  cardid INT(11) NOT NULL,
  setcode VARCHAR(3) DEFAULT NULL,
  datemodified DATETIME NOT NULL,
  difference INT(11) DEFAULT NULL,
  PRIMARY KEY (id)
) 