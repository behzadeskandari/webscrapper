// init-mongo.js

// Switch to admin database to create user
db = db.getSiblingDB("admin");

// Create user in admin database with access to scraper database
db.createUser({
  user: "myuser",
  pwd: "mypassword",
  roles: [
    { role: "readWrite", db: "scraper" },
    { role: "dbAdmin", db: "scraper" }
  ]
});

// Switch to scraper database
db = db.getSiblingDB("scraper");

// Create the Properties collection
db.createCollection("Properties");
