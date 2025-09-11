// Switch to admin database to create user
db = db.getSiblingDB("admin");

// Check if user already exists before creating
if (!db.getUser("myuser")) {
    db.createUser({
        user: "myuser",
        pwd: "mypassword",
        roles: [
            { role: "readWrite", db: "scraper" },
            { role: "dbAdmin", db: "scraper" }
        ],
        mechanisms: ["SCRAM-SHA-1", "SCRAM-SHA-256"]
    });
}

// Switch to scraper database
db = db.getSiblingDB("scraper");

// Create the Properties collection if it doesn't exist
if (!db.getCollection("Properties")) {
    db.createCollection("Properties");
}