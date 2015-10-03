CREATE TABLE usercards (
  id INT(11) NOT NULL AUTO_INCREMENT,
  ownerid INT(11) NOT NULL,
  cardid INT(11) NOT NULL,
  setcode VARCHAR(3) NOT NULL,
  count INT(11) NOT NULL,
  PRIMARY KEY (id)
) 