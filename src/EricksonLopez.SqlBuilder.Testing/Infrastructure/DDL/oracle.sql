-- =============================================================================
-- EricksonLopez.SqlBuilder — Oracle 19c+ DDL
-- Version: 1.0.0 | Target: Oracle 19c+ / Oracle 21c
-- =============================================================================
-- Notes:
--   • No AUTO_INCREMENT: use SEQUENCE + TRIGGER or IDENTITY (Oracle 12c+)
--     Strategy: use GENERATED ALWAYS AS IDENTITY (Oracle 12c+ SQL standard)
--   • VARCHAR2 instead of VARCHAR (Oracle-specific, max 4000 bytes or 32767 with MAX_STRING_SIZE=EXTENDED)
--   • NUMBER(1,0) for boolean (no native BOOLEAN in SQL, only in PL/SQL)
--   • TIMESTAMP WITH TIME ZONE for timezone-aware datetimes
--   • CLOB for large text (> 4000 chars), VARCHAR2(4000) for shorter strings
--   • No native JSON type before Oracle 21c: use VARCHAR2(4000) + IS JSON constraint (12.2+)
--     Oracle 21c has native JSON data type
--   • CHAR(n) pads with spaces; use VARCHAR2(n) always
--   • NULLs in Oracle: empty string '' is treated as NULL
--   • DUAL pseudo-table for expressions: SELECT SYSDATE FROM DUAL
--   • ROWNUM / ROW_NUMBER() OVER for pagination (not LIMIT/OFFSET before 12c)
--   • MERGE INTO is native and powerful (Oracle 9i+)
--   • Sequences: CREATE SEQUENCE ... START WITH 1 INCREMENT BY 1
--   • GLOBAL TEMPORARY TABLES for session-scoped temp data
-- =============================================================================

-- ─── Drop existing objects ────────────────────────────────────────────────────
BEGIN
    FOR obj IN (SELECT object_name, object_type FROM user_objects
                WHERE object_type IN ('TABLE','SEQUENCE')
                AND object_name IN (
                    'AUDIT_LOGS','USER_ROLES','PAYMENTS','INVOICES',
                    'ORDER_ITEMS','ORDERS','PRODUCTS','CATEGORIES',
                    'ADDRESSES','CUSTOMERS','ROLES','USERS',
                    'SEQ_CUSTOMERS','SEQ_ADDRESSES','SEQ_CATEGORIES',
                    'SEQ_PRODUCTS','SEQ_ORDERS','SEQ_ORDER_ITEMS',
                    'SEQ_INVOICES','SEQ_PAYMENTS','SEQ_USERS',
                    'SEQ_ROLES','SEQ_AUDIT_LOGS'
                ))
    LOOP
        IF obj.object_type = 'TABLE' THEN
            EXECUTE IMMEDIATE 'DROP TABLE "' || obj.object_name || '" CASCADE CONSTRAINTS PURGE';
        ELSIF obj.object_type = 'SEQUENCE' THEN
            EXECUTE IMMEDIATE 'DROP SEQUENCE "' || obj.object_name || '"';
        END IF;
    END LOOP;
END;
/

-- ─── Customers ────────────────────────────────────────────────────────────────
CREATE TABLE "customers" (
    "id"         NUMBER         GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name"       VARCHAR2(255)  NOT NULL,
    "email"      VARCHAR2(320)  NOT NULL CONSTRAINT uq_customers_email UNIQUE,
    "phone"      VARCHAR2(50),
    "tax_id"     VARCHAR2(50),
    "is_active"  NUMBER(1,0)    NOT NULL DEFAULT 1 CONSTRAINT chk_cust_active CHECK ("is_active" IN (0,1)),
    "created_at" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "updated_at" TIMESTAMP WITH TIME ZONE
);

CREATE INDEX "idx_customers_email"      ON "customers"("email");
CREATE INDEX "idx_customers_is_active"  ON "customers"("is_active");
CREATE INDEX "idx_customers_created_at" ON "customers"("created_at" DESC);

COMMENT ON TABLE  "customers"            IS 'Business and individual customers';
COMMENT ON COLUMN "customers"."is_active" IS '1=active, 0=inactive';

-- ─── Addresses ────────────────────────────────────────────────────────────────
CREATE TABLE "addresses" (
    "id"           NUMBER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "customer_id"  NUMBER       NOT NULL CONSTRAINT fk_addr_customer REFERENCES "customers"("id") ON DELETE CASCADE,
    "street"       VARCHAR2(500) NOT NULL,
    "city"         VARCHAR2(100) NOT NULL,
    "state"        VARCHAR2(100) NOT NULL,
    "country"      VARCHAR2(100) NOT NULL DEFAULT 'US',
    "postal_code"  VARCHAR2(20)  NOT NULL,
    "address_type" VARCHAR2(20)  NOT NULL DEFAULT 'shipping'
                   CONSTRAINT chk_addr_type CHECK ("address_type" IN ('shipping','billing')),
    "is_default"   NUMBER(1,0)   NOT NULL DEFAULT 0
                   CONSTRAINT chk_addr_default CHECK ("is_default" IN (0,1))
);

CREATE INDEX "idx_addresses_customer_id" ON "addresses"("customer_id");

-- ─── Categories ───────────────────────────────────────────────────────────────
CREATE TABLE "categories" (
    "id"                 NUMBER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name"               VARCHAR2(200) NOT NULL,
    "slug"               VARCHAR2(200) NOT NULL CONSTRAINT uq_categories_slug UNIQUE,
    "parent_category_id" NUMBER       CONSTRAINT fk_cat_parent REFERENCES "categories"("id") ON DELETE SET NULL,
    "is_active"          NUMBER(1,0)   NOT NULL DEFAULT 1 CONSTRAINT chk_cat_active CHECK ("is_active" IN (0,1)),
    "sort_order"         NUMBER        NOT NULL DEFAULT 0
);

CREATE INDEX "idx_categories_parent_id" ON "categories"("parent_category_id");
CREATE INDEX "idx_categories_slug"      ON "categories"("slug");

-- ─── Products ─────────────────────────────────────────────────────────────────
CREATE TABLE "products" (
    "id"          NUMBER         GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "category_id" NUMBER         NOT NULL CONSTRAINT fk_prod_category REFERENCES "categories"("id") ON DELETE RESTRICT,
    "name"        VARCHAR2(400)  NOT NULL,
    "sku"         VARCHAR2(100)  NOT NULL CONSTRAINT uq_products_sku UNIQUE,
    "description" CLOB,
    "price"       NUMBER(18,4)   NOT NULL CONSTRAINT chk_prod_price CHECK ("price" >= 0),
    "cost_price"  NUMBER(18,4)   NOT NULL DEFAULT 0 CONSTRAINT chk_cost_price CHECK ("cost_price" >= 0),
    "stock"       NUMBER         NOT NULL DEFAULT 0,
    "min_stock"   NUMBER         NOT NULL DEFAULT 0,
    "is_active"   NUMBER(1,0)    NOT NULL DEFAULT 1 CONSTRAINT chk_prod_active CHECK ("is_active" IN (0,1)),
    "created_at"  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "updated_at"  TIMESTAMP WITH TIME ZONE
);

CREATE INDEX "idx_products_category_id" ON "products"("category_id");
CREATE INDEX "idx_products_sku"         ON "products"("sku");
CREATE INDEX "idx_products_price"       ON "products"("price");
CREATE INDEX "idx_products_is_active"   ON "products"("is_active");

-- ─── Orders ───────────────────────────────────────────────────────────────────
CREATE TABLE "orders" (
    "id"              NUMBER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "customer_id"     NUMBER       NOT NULL CONSTRAINT fk_orders_customer REFERENCES "customers"("id") ON DELETE RESTRICT,
    "status"          VARCHAR2(20) NOT NULL DEFAULT 'pending'
                      CONSTRAINT chk_order_status CHECK ("status" IN ('pending','confirmed','shipped','delivered','cancelled')),
    "notes"           CLOB,
    "total_amount"    NUMBER(18,4) NOT NULL CONSTRAINT chk_order_total CHECK ("total_amount" >= 0),
    "tax_amount"      NUMBER(18,4) NOT NULL DEFAULT 0,
    "discount_amount" NUMBER(18,4) NOT NULL DEFAULT 0,
    "currency"        CHAR(3)      NOT NULL DEFAULT 'USD',
    "created_at"      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "confirmed_at"    TIMESTAMP WITH TIME ZONE,
    "shipped_at"      TIMESTAMP WITH TIME ZONE,
    "delivered_at"    TIMESTAMP WITH TIME ZONE,
    "is_deleted"      NUMBER(1,0)  NOT NULL DEFAULT 0 CONSTRAINT chk_ord_deleted CHECK ("is_deleted" IN (0,1)),
    "deleted_at"      TIMESTAMP WITH TIME ZONE
);

CREATE INDEX "idx_orders_customer_id" ON "orders"("customer_id");
CREATE INDEX "idx_orders_status"      ON "orders"("status");
CREATE INDEX "idx_orders_created_at"  ON "orders"("created_at" DESC);
CREATE INDEX "idx_orders_not_deleted" ON "orders"("customer_id","created_at" DESC) WHERE "is_deleted" = 0;

-- ─── Order Items ──────────────────────────────────────────────────────────────
CREATE TABLE "order_items" (
    "id"               NUMBER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "order_id"         NUMBER       NOT NULL CONSTRAINT fk_oi_order REFERENCES "orders"("id") ON DELETE CASCADE,
    "product_id"       NUMBER       NOT NULL CONSTRAINT fk_oi_product REFERENCES "products"("id") ON DELETE RESTRICT,
    "quantity"         NUMBER       NOT NULL CONSTRAINT chk_oi_qty CHECK ("quantity" > 0),
    "unit_price"       NUMBER(18,4) NOT NULL CONSTRAINT chk_oi_price CHECK ("unit_price" >= 0),
    "discount_percent" NUMBER(5,2)  NOT NULL DEFAULT 0 CONSTRAINT chk_oi_disc CHECK ("discount_percent" BETWEEN 0 AND 100),
    "total_price"      NUMBER(18,4) NOT NULL CONSTRAINT chk_oi_total CHECK ("total_price" >= 0),
    "notes"            VARCHAR2(4000)
);

CREATE INDEX "idx_order_items_order_id"   ON "order_items"("order_id");
CREATE INDEX "idx_order_items_product_id" ON "order_items"("product_id");

-- ─── Invoices ─────────────────────────────────────────────────────────────────
CREATE TABLE "invoices" (
    "id"              NUMBER        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "order_id"        NUMBER        NOT NULL CONSTRAINT fk_inv_order REFERENCES "orders"("id") ON DELETE RESTRICT,
    "invoice_number"  VARCHAR2(100) NOT NULL CONSTRAINT uq_invoices_number UNIQUE,
    "status"          VARCHAR2(20)  NOT NULL DEFAULT 'draft'
                      CONSTRAINT chk_inv_status CHECK ("status" IN ('draft','issued','paid','overdue','cancelled')),
    "subtotal_amount" NUMBER(18,4)  NOT NULL,
    "tax_amount"      NUMBER(18,4)  NOT NULL DEFAULT 0,
    "total_amount"    NUMBER(18,4)  NOT NULL,
    "paid_amount"     NUMBER(18,4)  NOT NULL DEFAULT 0,
    "currency"        CHAR(3)       NOT NULL DEFAULT 'USD',
    "issued_at"       TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "due_at"          TIMESTAMP WITH TIME ZONE NOT NULL,
    "paid_at"         TIMESTAMP WITH TIME ZONE,
    "notes"           CLOB
);

CREATE INDEX "idx_invoices_order_id" ON "invoices"("order_id");
CREATE INDEX "idx_invoices_status"   ON "invoices"("status");
CREATE INDEX "idx_invoices_due_at"   ON "invoices"("due_at");

-- ─── Payments ─────────────────────────────────────────────────────────────────
CREATE TABLE "payments" (
    "id"               NUMBER        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "invoice_id"       NUMBER        NOT NULL CONSTRAINT fk_pay_invoice REFERENCES "invoices"("id") ON DELETE RESTRICT,
    "amount"           NUMBER(18,4)  NOT NULL CONSTRAINT chk_pay_amount CHECK ("amount" > 0),
    "method"           VARCHAR2(30)  NOT NULL CONSTRAINT chk_pay_method CHECK ("method" IN ('credit_card','bank_transfer','paypal','stripe','cash','check')),
    "status"           VARCHAR2(20)  NOT NULL DEFAULT 'pending'
                       CONSTRAINT chk_pay_status CHECK ("status" IN ('pending','completed','failed','refunded')),
    "transaction_ref"  VARCHAR2(200),
    "gateway_response" CLOB,
    "paid_at"          TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "refunded_at"      TIMESTAMP WITH TIME ZONE,
    "refunded_amount"  NUMBER(18,4)
);

CREATE INDEX "idx_payments_invoice_id"      ON "payments"("invoice_id");
CREATE INDEX "idx_payments_status"          ON "payments"("status");
CREATE INDEX "idx_payments_transaction_ref" ON "payments"("transaction_ref");

-- ─── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE "users" (
    "id"                    NUMBER        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "username"              VARCHAR2(100) NOT NULL CONSTRAINT uq_users_username UNIQUE,
    "email"                 VARCHAR2(320) NOT NULL CONSTRAINT uq_users_email UNIQUE,
    "password_hash"         VARCHAR2(500) NOT NULL,
    "first_name"            VARCHAR2(100),
    "last_name"             VARCHAR2(100),
    "is_active"             NUMBER(1,0)   NOT NULL DEFAULT 1 CONSTRAINT chk_usr_active CHECK ("is_active" IN (0,1)),
    "email_verified"        NUMBER(1,0)   NOT NULL DEFAULT 0 CONSTRAINT chk_email_ver CHECK ("email_verified" IN (0,1)),
    "created_at"            TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "last_login_at"         TIMESTAMP WITH TIME ZONE,
    "locked_until"          TIMESTAMP WITH TIME ZONE,
    "failed_login_attempts" NUMBER        NOT NULL DEFAULT 0
);

CREATE INDEX "idx_users_username"  ON "users"("username");
CREATE INDEX "idx_users_email"     ON "users"("email");
CREATE INDEX "idx_users_is_active" ON "users"("is_active");

-- ─── Roles ────────────────────────────────────────────────────────────────────
CREATE TABLE "roles" (
    "id"          NUMBER        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "name"        VARCHAR2(100) NOT NULL CONSTRAINT uq_roles_name UNIQUE,
    "description" VARCHAR2(500),
    -- Oracle 12.2+: JSON stored as VARCHAR2/CLOB with IS JSON constraint
    -- Oracle 21c+: use JSON data type directly
    "permissions" VARCHAR2(4000) NOT NULL DEFAULT '[]'
                  CONSTRAINT chk_roles_json CHECK ("permissions" IS JSON),
    "is_system"   NUMBER(1,0)   NOT NULL DEFAULT 0 CONSTRAINT chk_role_sys CHECK ("is_system" IN (0,1)),
    "created_at"  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP
);

CREATE INDEX "idx_roles_name" ON "roles"("name");

-- ─── User Roles ───────────────────────────────────────────────────────────────
CREATE TABLE "user_roles" (
    "user_id"             NUMBER NOT NULL CONSTRAINT fk_ur_user REFERENCES "users"("id") ON DELETE CASCADE,
    "role_id"             NUMBER NOT NULL CONSTRAINT fk_ur_role REFERENCES "roles"("id") ON DELETE CASCADE,
    "assigned_at"         TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "assigned_by_user_id" NUMBER CONSTRAINT fk_ur_assigned_by REFERENCES "users"("id") ON DELETE SET NULL,
    CONSTRAINT pk_user_roles PRIMARY KEY ("user_id", "role_id")
);

CREATE INDEX "idx_user_roles_user_id" ON "user_roles"("user_id");
CREATE INDEX "idx_user_roles_role_id" ON "user_roles"("role_id");

-- ─── Audit Logs ───────────────────────────────────────────────────────────────
CREATE TABLE "audit_logs" (
    "id"             NUMBER        GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "entity_name"    VARCHAR2(100) NOT NULL,
    "entity_id"      VARCHAR2(100) NOT NULL,
    "action"         VARCHAR2(10)  NOT NULL CONSTRAINT chk_audit_action CHECK ("action" IN ('INSERT','UPDATE','DELETE')),
    "old_values"     CLOB          CONSTRAINT chk_old_json CHECK ("old_values" IS JSON OR "old_values" IS NULL),
    "new_values"     CLOB          CONSTRAINT chk_new_json CHECK ("new_values" IS JSON OR "new_values" IS NULL),
    "changed_fields" VARCHAR2(4000) CONSTRAINT chk_changed_json CHECK ("changed_fields" IS JSON OR "changed_fields" IS NULL),
    "user_id"        NUMBER        CONSTRAINT fk_audit_user REFERENCES "users"("id") ON DELETE SET NULL,
    "ip_address"     VARCHAR2(45),
    "user_agent"     VARCHAR2(500),
    "timestamp"      TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT SYSTIMESTAMP,
    "correlation_id" VARCHAR2(200)
);

CREATE INDEX "idx_audit_entity"    ON "audit_logs"("entity_name", "entity_id");
CREATE INDEX "idx_audit_timestamp" ON "audit_logs"("timestamp" DESC);
CREATE INDEX "idx_audit_user_id"   ON "audit_logs"("user_id");
CREATE INDEX "idx_audit_action"    ON "audit_logs"("action");

-- ─── Global Temporary Table Example ──────────────────────────────────────────
-- Oracle GTT: data exists only for session duration (ON COMMIT DELETE ROWS = transaction scope)
CREATE GLOBAL TEMPORARY TABLE "temp_order_summary" (
    "customer_id"   NUMBER,
    "order_count"   NUMBER,
    "total_revenue" NUMBER(18,4)
) ON COMMIT DELETE ROWS;

-- ─── Seed Data ────────────────────────────────────────────────────────────────
INSERT INTO "roles" ("name", "description", "permissions", "is_system") VALUES
    ('admin',   'Full system administrator', '["*"]', 1);
INSERT INTO "roles" ("name", "description", "permissions", "is_system") VALUES
    ('manager', 'Can manage orders, products, customers', '["orders:*","products:*","customers:*"]', 1);
INSERT INTO "roles" ("name", "description", "permissions", "is_system") VALUES
    ('viewer',  'Read-only access', '["orders:read","products:read","customers:read"]', 1);
INSERT INTO "roles" ("name", "description", "permissions", "is_system") VALUES
    ('support', 'Customer support role', '["orders:read","customers:*"]', 0);

INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Electronics',  'electronics', NULL, 1);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Clothing',     'clothing',    NULL, 2);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Home & Garden','home-garden', NULL, 3);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Books & Media','books-media', NULL, 4);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Laptops',      'laptops',     1,    1);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Smartphones',  'smartphones', 1,    2);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Accessories',  'accessories', 1,    3);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Mens Clothing','mens-clothing',2,   1);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Womens Clothing','womens-clothing',2,2);
INSERT INTO "categories" ("name", "slug", "parent_category_id", "sort_order") VALUES ('Gaming Laptops','gaming-laptops',5, 1);

COMMIT;
