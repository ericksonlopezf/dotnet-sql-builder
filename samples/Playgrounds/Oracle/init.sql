-- Oracle Free 23c init script for SqlBuilder Playground
-- Runs as APP_USER (demo) — schema already exists

-- ─── CLEAN EXISTING OBJECTS (idempotent) ──────────────────────────────────────

BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "order_items"  CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "orders"       CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "products"     CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "customers"    CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/
BEGIN
    EXECUTE IMMEDIATE 'DROP TABLE "categories"   CASCADE CONSTRAINTS';
EXCEPTION WHEN OTHERS THEN NULL;
END;
/

-- ─── SCHEMA ──────────────────────────────────────────────────────────────────

CREATE TABLE "categories" (
    "id"        NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name"      VARCHAR2(150)   NOT NULL,
    "slug"      VARCHAR2(150)   NOT NULL UNIQUE,
    "is_active" NUMBER(1,0)     DEFAULT 1 NOT NULL
);

CREATE TABLE "customers" (
    "id"         NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name"       VARCHAR2(250)   NOT NULL,
    "email"      VARCHAR2(320)   NOT NULL UNIQUE,
    "phone"      VARCHAR2(30),
    "is_active"  NUMBER(1,0)     DEFAULT 1 NOT NULL,
    "created_at" TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL
);

CREATE TABLE "products" (
    "id"          NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "category_id" NUMBER          NOT NULL REFERENCES "categories"("id"),
    "name"        VARCHAR2(250)   NOT NULL,
    "sku"         VARCHAR2(100)   NOT NULL UNIQUE,
    "price"       NUMBER(18,4)    NOT NULL,
    "cost_price"  NUMBER(18,4)    DEFAULT 0 NOT NULL,
    "stock"       NUMBER          DEFAULT 0 NOT NULL,
    "is_active"   NUMBER(1,0)     DEFAULT 1 NOT NULL
);

CREATE TABLE "orders" (
    "id"           NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "customer_id"  NUMBER          NOT NULL REFERENCES "customers"("id"),
    "status"       VARCHAR2(20)    DEFAULT 'pending' NOT NULL
                   CONSTRAINT chk_order_status CHECK ("status" IN ('pending','confirmed','shipped','delivered','cancelled')),
    "total_amount" NUMBER(18,4)    DEFAULT 0 NOT NULL,
    "currency"     CHAR(3)         DEFAULT 'USD' NOT NULL,
    "is_deleted"   NUMBER(1,0)     DEFAULT 0 NOT NULL,
    "created_at"   TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL
);

CREATE TABLE "order_items" (
    "id"          NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "order_id"    NUMBER          NOT NULL REFERENCES "orders"("id") ON DELETE CASCADE,
    "product_id"  NUMBER          NOT NULL REFERENCES "products"("id"),
    "quantity"    NUMBER          NOT NULL,
    "unit_price"  NUMBER(18,4)    DEFAULT 0 NOT NULL,
    "total_price" NUMBER(18,4)    DEFAULT 0 NOT NULL
);

-- ─── INDEXES ─────────────────────────────────────────────────────────────────

CREATE INDEX idx_orders_customer ON "orders"("customer_id");
CREATE INDEX idx_order_items_order ON "order_items"("order_id");
CREATE INDEX idx_order_items_product ON "order_items"("product_id");
CREATE INDEX idx_products_category ON "products"("category_id");
CREATE INDEX idx_customers_email ON "customers"("email");

-- ─── SEED: Categories ─────────────────────────────────────────────────────────

INSERT INTO "categories" ("name", "slug", "is_active") VALUES ('Electronics', 'electronics', 1);
INSERT INTO "categories" ("name", "slug", "is_active") VALUES ('Laptops', 'laptops', 1);
INSERT INTO "categories" ("name", "slug", "is_active") VALUES ('Smartphones', 'smartphones', 1);
INSERT INTO "categories" ("name", "slug", "is_active") VALUES ('Audio', 'audio', 1);
INSERT INTO "categories" ("name", "slug", "is_active") VALUES ('Gaming', 'gaming', 1);

-- ─── SEED: Customers ─────────────────────────────────────────────────────────

INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Acme Corporation',    'billing@acme.corp',              '+1-555-0100', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Globex Industries',   'orders@globex.com',              '+1-555-0101', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Stark Industries',    'tony@stark.io',                  '+1-555-0104', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Wayne Enterprises',   'bruce@wayne.biz',                '+1-555-0105', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Cyberdyne Systems',   'support@cyberdyne.ai',           '+1-555-0110', 0);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Dunder Mifflin',      'michael@dundermifflin.biz',      '+1-555-0117', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Pied Piper LLC',      'richard@piedpiper.com',          '+1-555-0118', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Hooli Corp',          'gavin@hooli.com',                '+1-555-0119', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Nakatomi Trading',    'billing@nakatomi.jp',            '+1-555-0112', 1);
INSERT INTO "customers" ("name", "email", "phone", "is_active") VALUES ('Massive Dynamic',     'peter@massivedynamic.io',        '+1-555-0113', 1);

-- ─── SEED: Products ──────────────────────────────────────────────────────────

INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (2, 'MacBook Pro 16 M4', 'LAPTOP-MBP16-M4', 3499.0000, 2800.0000, 25, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (2, 'Dell XPS 15 OLED', 'LAPTOP-XPS15-OLED', 2199.0000, 1700.0000, 18, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (3, 'iPhone 16 Pro Max', 'PHONE-IP16PM-256', 1199.0000, 900.0000, 50, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (3, 'Samsung Galaxy S25', 'PHONE-SGS25U', 1099.0000, 820.0000, 45, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (4, 'Sony WH-1000XM6', 'AUDIO-WH1000XM6', 399.0000, 290.0000, 60, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (4, 'Apple AirPods Pro 3', 'AUDIO-APP3', 249.0000, 180.0000, 80, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (5, 'PlayStation 5 Slim', 'GAMING-PS5S', 449.0000, 380.0000, 30, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (5, 'Nintendo Switch OLED', 'GAMING-NSWITCH', 349.0000, 270.0000, 35, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (1, 'Anker 200W Charger', 'ACC-ANKER200W', 89.9900, 55.0000, 100, 1);
INSERT INTO "products" ("category_id", "name", "sku", "price", "cost_price", "stock", "is_active")
    VALUES (2, 'ThinkPad X1 Carbon', 'LAPTOP-TP-X1-G12', 1899.0000, 1500.0000, 30, 1);

-- ─── SEED: Orders ────────────────────────────────────────────────────────────

INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (1, 'delivered', 4698.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (1, 'shipped',   3748.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (2, 'confirmed', 1248.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (2, 'pending',    449.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (3, 'delivered', 5998.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (4, 'shipped',   2198.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (5, 'cancelled',    0.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (6, 'confirmed', 1399.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (7, 'delivered', 3698.0000, 'USD', 0);
INSERT INTO "orders" ("customer_id", "status", "total_amount", "currency", "is_deleted")
    VALUES (8, 'pending',    898.0000, 'USD', 0);

-- ─── SEED: Order Items ───────────────────────────────────────────────────────

INSERT INTO "order_items" ("order_id", "product_id", "quantity", "unit_price", "total_price")
    VALUES (1, 1, 1, 3499.0000, 3499.0000);
INSERT INTO "order_items" ("order_id", "product_id", "quantity", "unit_price", "total_price")
    VALUES (1, 6, 1, 249.0000, 249.0000);
INSERT INTO "order_items" ("order_id", "product_id", "quantity", "unit_price", "total_price")
    VALUES (2, 2, 1, 2199.0000, 2199.0000);
INSERT INTO "order_items" ("order_id", "product_id", "quantity", "unit_price", "total_price")
    VALUES (2, 5, 1, 399.0000, 399.0000);
INSERT INTO "order_items" ("order_id", "product_id", "quantity", "unit_price", "total_price")
    VALUES (3, 3, 1, 1199.0000, 1199.0000);

COMMIT;
