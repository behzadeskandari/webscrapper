// init-mongo.js

db = db.getSiblingDB("scraper");
db.createUser({
  user: "myuser",
  pwd: "mypassword",
  roles: [{ role: "readWrite", db: "scraper" }]
});
