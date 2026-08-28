-- =============================================================================
-- EricksonLopez.SqlBuilder — SQLite DDL
-- Version: 1.0.0 | Target: SQLite 3.35+ (RETURNING support)
-- =============================================================================
-- Notes:
--   • INTEGER PRIMARY KEY is implicitly the ROWID (64-bit signed integer)
--   • AUTOINCREMENT keyword is optional — avoid unless preventing rowid reuse is required
--   • No native BOOLEAN: use INTEGER 0/1 (SQLite type affinity)
--   • No native DATETIME: use TEXT (ISO 8601) or REAL (Julian day) or INTEGER (Unix epoch)
--     Convention here: TEXT with ISO 8601 format, e.g. '2026-07-27T10:00:00Z'
--   • No native UUID: use TEXT
--   • No CHAR(n) enforcement: SQLite is typeless (type affinity system)
--   • FOREIGN KEYS must be enabled per connection: PRAGMA foreign_keys = ON;
--   • RETURNING clause available since SQLite 3.35.0 (March 2021)
--   • ON CONFLICT DO UPDATE (UPSERT) available since SQLite 3.24.0 (June 2018)
--   • WITHOUT ROWID tables available for composite PK tables (e.g. user_roles)
--   • No partial indexes with expressions (use WHERE clause on index)
--   • JSON1 extension available for JSON functions (json_extract, json_each, etc.)
-- =============================================================================

PRAGMA journal_mode=WAL;       -- Better concurrency for reads
PRAGMA foreign_keys=ON;
PRAGMA synchronous=NORMAL;     -- Balance between safety and performance

-- ─── Drop existing tables ─────────────────────────────────────────────────────
DROP TABLE IF EXISTS "audit_logs";
DROP TABLE IF EXISTS "user_roles";
DROP TABLE IF EXISTS "payments";
DROP TABLE IF EXISTS "invoices";
DROP TABLE IF EXISTS "order_items";
DROP TABLE IF EXISTS "orders";
DROP TABLE IF EXISTS "products";
DROP TABLE IF EXISTS "categories";
DROP TABLE IF EXISTS "addresses";
DROP TABLE IF EXISTS "customers";
DROP TABLE IF EXISTS "roles";
DROP TABLE IF EXISTS "users";

-- ─── Customers ────────────────────────────────────────────────────────────────
CREATE TABLE "customers" (
    "id"         INTEGER PRIMARY KEY,           -- Implicit ROWID alias; no AUTOINCREMENT to allow row ID reuse
    "name"       TEXT    NOT NULL,
    "email"      TEXT    NOT NULL UNIQUE,
    "phone"      TEXT,
    "tax_id"     TEXT,
    "is_active"  INTEGER NOT NULL DEFAULT 1,    -- 0=inactive, 1=active
    "created_at" TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "updated_at" TEXT,
    CHECK ("is_active" IN (0, 1))
);

CREATE INDEX "idx_customers_email"      ON "customers"("email");
CREATE INDEX "idx_customers_is_active"  ON "customers"("is_active");
CREATE INDEX "idx_customers_created_at" ON "customers"("created_at" DESC);

-- ─── Addresses ────────────────────────────────────────────────────────────────
CREATE TABLE "addresses" (
    "id"           INTEGER PRIMARY KEY,
    "customer_id"  INTEGER NOT NULL REFERENCES "customers"("id") ON DELETE CASCADE,
    "street"       TEXT    NOT NULL,
    "city"         TEXT    NOT NULL,
    "state"        TEXT    NOT NULL,
    "country"      TEXT    NOT NULL DEFAULT 'US',
    "postal_code"  TEXT    NOT NULL,
    "address_type" TEXT    NOT NULL DEFAULT 'shipping' CHECK ("address_type" IN ('shipping','billing')),
    "is_default"   INTEGER NOT NULL DEFAULT 0 CHECK ("is_default" IN (0,1))
);

CREATE INDEX "idx_addresses_customer_id" ON "addresses"("customer_id");

-- ─── Categories ───────────────────────────────────────────────────────────────
CREATE TABLE "categories" (
    "id"                 INTEGER PRIMARY KEY,
    "name"               TEXT    NOT NULL,
    "slug"               TEXT    NOT NULL UNIQUE,
    "parent_category_id" INTEGER REFERENCES "categories"("id") ON DELETE SET NULL,
    "is_active"          INTEGER NOT NULL DEFAULT 1 CHECK ("is_active" IN (0,1)),
    "sort_order"         INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX "idx_categories_parent_id" ON "categories"("parent_category_id");
CREATE INDEX "idx_categories_slug"      ON "categories"("slug");

-- ─── Products ─────────────────────────────────────────────────────────────────
CREATE TABLE "products" (
    "id"          INTEGER PRIMARY KEY,
    "category_id" INTEGER NOT NULL REFERENCES "categories"("id") ON DELETE RESTRICT,
    "name"        TEXT    NOT NULL,
    "sku"         TEXT    NOT NULL UNIQUE,
    "description" TEXT,
    "price"       REAL    NOT NULL CHECK ("price" >= 0),
    "cost_price"  REAL    NOT NULL DEFAULT 0.0 CHECK ("cost_price" >= 0),
    "stock"       INTEGER NOT NULL DEFAULT 0,
    "min_stock"   INTEGER NOT NULL DEFAULT 0,
    "is_active"   INTEGER NOT NULL DEFAULT 1 CHECK ("is_active" IN (0,1)),
    "created_at"  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "updated_at"  TEXT
);

CREATE INDEX "idx_products_category_id" ON "products"("category_id");
CREATE INDEX "idx_products_sku"         ON "products"("sku");
CREATE INDEX "idx_products_price"       ON "products"("price");
CREATE INDEX "idx_products_is_active"   ON "products"("is_active") WHERE "is_active" = 1;

-- ─── Orders ───────────────────────────────────────────────────────────────────
CREATE TABLE "orders" (
    "id"              INTEGER PRIMARY KEY,
    "customer_id"     INTEGER NOT NULL REFERENCES "customers"("id") ON DELETE RESTRICT,
    "status"          TEXT    NOT NULL DEFAULT 'pending'
                      CHECK ("status" IN ('pending','confirmed','shipped','delivered','cancelled')),
    "notes"           TEXT,
    "total_amount"    REAL    NOT NULL CHECK ("total_amount" >= 0),
    "tax_amount"      REAL    NOT NULL DEFAULT 0.0,
    "discount_amount" REAL    NOT NULL DEFAULT 0.0,
    "currency"        TEXT    NOT NULL DEFAULT 'USD',
    "created_at"      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "confirmed_at"    TEXT,
    "shipped_at"      TEXT,
    "delivered_at"    TEXT,
    "is_deleted"      INTEGER NOT NULL DEFAULT 0 CHECK ("is_deleted" IN (0,1)),
    "deleted_at"      TEXT
);

CREATE INDEX "idx_orders_customer_id" ON "orders"("customer_id");
CREATE INDEX "idx_orders_status"      ON "orders"("status");
CREATE INDEX "idx_orders_created_at"  ON "orders"("created_at" DESC);
-- Partial index: only non-deleted orders (SQLite 3.8.9+)
CREATE INDEX "idx_orders_active"      ON "orders"("customer_id","created_at" DESC) WHERE "is_deleted" = 0;

-- ─── Order Items ──────────────────────────────────────────────────────────────
CREATE TABLE "order_items" (
    "id"               INTEGER PRIMARY KEY,
    "order_id"         INTEGER NOT NULL REFERENCES "orders"("id") ON DELETE CASCADE,
    "product_id"       INTEGER NOT NULL REFERENCES "products"("id") ON DELETE RESTRICT,
    "quantity"         INTEGER NOT NULL CHECK ("quantity" > 0),
    "unit_price"       REAL    NOT NULL CHECK ("unit_price" >= 0),
    "discount_percent" REAL    NOT NULL DEFAULT 0.0 CHECK ("discount_percent" BETWEEN 0 AND 100),
    "total_price"      REAL    NOT NULL CHECK ("total_price" >= 0),
    "notes"            TEXT
);

CREATE INDEX "idx_order_items_order_id"   ON "order_items"("order_id");
CREATE INDEX "idx_order_items_product_id" ON "order_items"("product_id");

-- ─── Invoices ─────────────────────────────────────────────────────────────────
CREATE TABLE "invoices" (
    "id"              INTEGER PRIMARY KEY,
    "order_id"        INTEGER NOT NULL REFERENCES "orders"("id") ON DELETE RESTRICT,
    "invoice_number"  TEXT    NOT NULL UNIQUE,
    "status"          TEXT    NOT NULL DEFAULT 'draft'
                      CHECK ("status" IN ('draft','issued','paid','overdue','cancelled')),
    "subtotal_amount" REAL    NOT NULL,
    "tax_amount"      REAL    NOT NULL DEFAULT 0.0,
    "total_amount"    REAL    NOT NULL,
    "paid_amount"     REAL    NOT NULL DEFAULT 0.0,
    "currency"        TEXT    NOT NULL DEFAULT 'USD',
    "issued_at"       TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "due_at"          TEXT    NOT NULL,
    "paid_at"         TEXT,
    "notes"           TEXT
);

CREATE INDEX "idx_invoices_order_id" ON "invoices"("order_id");
CREATE INDEX "idx_invoices_status"   ON "invoices"("status");
CREATE INDEX "idx_invoices_due_at"   ON "invoices"("due_at") WHERE "status" NOT IN ('paid','cancelled');

-- ─── Payments ─────────────────────────────────────────────────────────────────
CREATE TABLE "payments" (
    "id"               INTEGER PRIMARY KEY,
    "invoice_id"       INTEGER NOT NULL REFERENCES "invoices"("id") ON DELETE RESTRICT,
    "amount"           REAL    NOT NULL CHECK ("amount" > 0),
    "method"           TEXT    NOT NULL CHECK ("method" IN ('credit_card','bank_transfer','paypal','stripe','cash','check')),
    "status"           TEXT    NOT NULL DEFAULT 'pending'
                       CHECK ("status" IN ('pending','completed','failed','refunded')),
    "transaction_ref"  TEXT,
    "gateway_response" TEXT,
    "paid_at"          TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "refunded_at"      TEXT,
    "refunded_amount"  REAL
);

CREATE INDEX "idx_payments_invoice_id"      ON "payments"("invoice_id");
CREATE INDEX "idx_payments_status"          ON "payments"("status");
CREATE INDEX "idx_payments_transaction_ref" ON "payments"("transaction_ref") WHERE "transaction_ref" IS NOT NULL;

-- ─── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE "users" (
    "id"                    INTEGER PRIMARY KEY,
    "username"              TEXT    NOT NULL UNIQUE,
    "email"                 TEXT    NOT NULL UNIQUE,
    "password_hash"         TEXT    NOT NULL,
    "first_name"            TEXT,
    "last_name"             TEXT,
    "is_active"             INTEGER NOT NULL DEFAULT 1 CHECK ("is_active" IN (0,1)),
    "email_verified"        INTEGER NOT NULL DEFAULT 0 CHECK ("email_verified" IN (0,1)),
    "created_at"            TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "last_login_at"         TEXT,
    "locked_until"          TEXT,
    "failed_login_attempts" INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX "idx_users_username"  ON "users"("username");
CREATE INDEX "idx_users_email"     ON "users"("email");
CREATE INDEX "idx_users_is_active" ON "users"("is_active") WHERE "is_active" = 1;

-- ─── Roles ────────────────────────────────────────────────────────────────────
CREATE TABLE "roles" (
    "id"          INTEGER PRIMARY KEY,
    "name"        TEXT    NOT NULL UNIQUE,
    "description" TEXT,
    "permissions" TEXT    NOT NULL DEFAULT '[]', -- JSON array stored as TEXT
    "is_system"   INTEGER NOT NULL DEFAULT 0 CHECK ("is_system" IN (0,1)),
    "created_at"  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE INDEX "idx_roles_name" ON "roles"("name");

-- ─── User Roles — WITHOUT ROWID for composite PK tables ───────────────────────
-- WITHOUT ROWID: more efficient for tables with composite PKs and no hidden rowid
CREATE TABLE "user_roles" (
    "user_id"             INTEGER NOT NULL REFERENCES "users"("id") ON DELETE CASCADE,
    "role_id"             INTEGER NOT NULL REFERENCES "roles"("id") ON DELETE CASCADE,
    "assigned_at"         TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "assigned_by_user_id" INTEGER REFERENCES "users"("id") ON DELETE SET NULL,
    PRIMARY KEY ("user_id", "role_id")
) WITHOUT ROWID;  -- SQLite optimization for composite PKs

CREATE INDEX "idx_user_roles_role_id" ON "user_roles"("role_id");

-- ─── Audit Logs ───────────────────────────────────────────────────────────────
CREATE TABLE "audit_logs" (
    "id"             INTEGER PRIMARY KEY,
    "entity_name"    TEXT    NOT NULL,
    "entity_id"      TEXT    NOT NULL,
    "action"         TEXT    NOT NULL CHECK ("action" IN ('INSERT','UPDATE','DELETE')),
    "old_values"     TEXT,   -- JSON stored as TEXT (use json() validation if needed)
    "new_values"     TEXT,   -- JSON stored as TEXT
    "changed_fields" TEXT,   -- JSON stored as TEXT
    "user_id"        INTEGER REFERENCES "users"("id") ON DELETE SET NULL,
    "ip_address"     TEXT,
    "user_agent"     TEXT,
    "timestamp"      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    "correlation_id" TEXT
);

CREATE INDEX "idx_audit_entity"    ON "audit_logs"("entity_name", "entity_id");
CREATE INDEX "idx_audit_timestamp" ON "audit_logs"("timestamp" DESC);
CREATE INDEX "idx_audit_user_id"   ON "audit_logs"("user_id") WHERE "user_id" IS NOT NULL;
CREATE INDEX "idx_audit_action"    ON "audit_logs"("action");

-- ─── Seed Data ────────────────────────────────────────────────────────────────
INSERT INTO "roles" ("name", "description", "permissions", "is_system") VALUES
    ('admin',   'Full system administrator', '["*"]', 1),
    ('manager', 'Can manage orders, products, customers', '["orders:*","products:*","customers:*"]', 1),
    ('viewer',  'Read-only access', '["orders:read","products:read","customers:read"]', 1),
    ('support', 'Customer support role', '["orders:read","customers:*"]', 0);

INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES
    ('Electronics',        'electronics',    NULL, 1),
    ('Clothing & Apparel', 'clothing',       NULL, 2),
    ('Home & Garden',      'home-garden',    NULL, 3),
    ('Books & Media',      'books-media',    NULL, 4),
    ('Laptops',            'laptops',        1,    1),
    ('Smartphones',        'smartphones',    1,    2),
    ('Accessories',        'accessories',    1,    3),
    ('Mens Clothing',      'mens-clothing',  2,    1),
    ('Womens Clothing',    'womens-clothing',2,    2),
    ('Gaming Laptops',     'gaming-laptops', 5,    1);
