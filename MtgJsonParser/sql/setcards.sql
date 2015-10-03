CREATE TABLE setcards (
  id INT(11) NOT NULL AUTO_INCREMENT,
  cardid INT(11) NOT NULL,
  setcode VARCHAR(3) NOT NULL,
  artist VARCHAR(50) NOT NULL,
  flavourtext TEXT,
  rarity VARCHAR(1) NOT NULL,
  collectornum INT(11) NOT NULL,
  PRIMARY KEY (id)
)