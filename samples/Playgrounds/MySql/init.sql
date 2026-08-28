-- MySQL 8.0 init script for SqlBuilder Playground

SET NAMES utf8mb4;
SET foreign_key_checks = 0;

CREATE TABLE IF NOT EXISTS `categories` (
    `id`        INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name`      VARCHAR(150) NOT NULL,
    `slug`      VARCHAR(150) NOT NULL UNIQUE,
    `is_active` TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `customers` (
    `id`         INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name`       VARCHAR(250) NOT NULL,
    `email`      VARCHAR(320) NOT NULL UNIQUE,
    `phone`      VARCHAR(30),
    `is_active`  TINYINT(1) NOT NULL DEFAULT 1,
    `created_at` DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `products` (
    `id`          INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `category_id` INT NOT NULL,
    `name`        VARCHAR(250) NOT NULL,
    `sku`         VARCHAR(100) NOT NULL UNIQUE,
    `price`       DECIMAL(18,4) NOT NULL,
    `cost_price`  DECIMAL(18,4) NOT NULL DEFAULT 0,
    `stock`       INT NOT NULL DEFAULT 0,
    `is_active`   TINYINT(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS `orders` (
    `id`           INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `customer_id`  INT NOT NULL,
    `status`       VARCHAR(20) NOT NULL DEFAULT 'pending',
    `total_amount` DECIMAL(18,4) NOT NULL DEFAULT 0,
    `currency`     CHAR(3) NOT NULL DEFAULT 'USD',
    `is_deleted`   TINYINT(1) NOT NULL DEFAULT 0,
    `created_at`   DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
    CONSTRAINT `fk_ord_cust` FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO `categories` VALUES
    (1,'Electronics','electronics',1),(2,'Laptops','laptops',1),
    (3,'Smartphones','smartphones',1),(4,'Audio','audio',1),(5,'Gaming','gaming',1);

INSERT IGNORE INTO `customers` (`name`,`email`,`phone`,`is_active`) VALUES
    ('Acme Corporation','billing@acme.corp','+1-555-0100',1),
    ('Globex Industries','orders@globex.com','+1-555-0101',1),
    ('Stark Industries','tony@stark.io','+1-555-0104',1),
    ('Wayne Enterprises','bruce@wayne.biz','+1-555-0105',1),
    ('Cyberdyne Systems','support@cyberdyne.ai','+1-555-0110',0),
    ('Dunder Mifflin','michael@dundermifflin.biz','+1-555-0117',1),
    ('Pied Piper LLC','richard@piedpiper.com','+1-555-0118',1),
    ('Hooli Corp','gavin@hooli.com','+1-555-0119',1),
    ('Nakatomi Trading','billing@nakatomi.jp','+1-555-0112',1),
    ('Massive Dynamic','peter@massivedynamic.io','+1-555-0113',1);

INSERT IGNORE INTO `products` (`category_id`,`name`,`sku`,`price`,`cost_price`,`stock`,`is_active`) VALUES
    (2,'MacBook Pro 16" M4','LAPTOP-MBP16-M4',3499.0000,2800.0000,25,1),
    (2,'Dell XPS 15 OLED','LAPTOP-XPS15-OLED',2199.0000,1700.0000,18,1),
    (3,'iPhone 16 Pro Max','PHONE-IP16PM-256',1199.0000,900.0000,50,1),
    (3,'Samsung Galaxy S25','PHONE-SGS25U',1099.0000,820.0000,45,1),
    (4,'Sony WH-1000XM6','AUDIO-WH1000XM6',399.0000,290.0000,60,1),
    (4,'Apple AirPods Pro 3','AUDIO-APP3',249.0000,180.0000,80,1),
    (5,'PlayStation 5 Slim','GAMING-PS5S',449.0000,380.0000,30,1),
    (5,'Nintendo Switch OLED','GAMING-NSWITCH',349.0000,270.0000,35,1),
    (1,'Anker 200W Charger','ACC-ANKER200W',89.9900,55.0000,100,1),
    (2,'ThinkPad X1 Carbon','LAPTOP-TP-X1-G12',1899.0000,1500.0000,30,1);

INSERT IGNORE INTO `orders` (`customer_id`,`status`,`total_amount`,`currency`,`is_deleted`) VALUES
    (1,'delivered',4698.0000,'USD',0),(1,'shipped',3748.0000,'USD',0),
    (2,'confirmed',1248.0000,'USD',0),(2,'pending',449.0000,'USD',0),
    (3,'delivered',5998.0000,'USD',0),(4,'shipped',2198.0000,'USD',0),
    (5,'cancelled',0.0000,'USD',0),(6,'confirmed',1399.0000,'USD',0),
    (7,'delivered',3698.0000,'USD',0),(8,'pending',898.0000,'USD',0);

SET foreign_key_checks = 1;
