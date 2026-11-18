-- Generated table animation_skill_actions (new in 3.29.3c); its rows come from sql/updates/client/. Written by scripts/dev/client-sql.ps1.
CREATE TABLE "animation_skill_actions" (
    "_row" INTEGER PRIMARY KEY,
    "rig" INTEGER,
    "stance" TEXT,
    "parent" TEXT,
    "action" TEXT,
    "ord" INTEGER,
    "animation" TEXT,
    "source" TEXT,
    "index" INTEGER,
    "begin" INTEGER,
    "length" INTEGER,
    "contact" INTEGER,
    "cancel" INTEGER,
    "slow" JSON,
    "budget" REAL,
    "item" BOOLEAN,
    "pair" BOOLEAN,
    "position" JSON,
    "effects" JSON
);
