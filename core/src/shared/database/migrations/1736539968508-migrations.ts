import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1736539968508 implements MigrationInterface {
  name = 'Migrations1736539968508';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `CREATE TABLE "categories" ("id" character varying NOT NULL, "name" character varying NOT NULL, "description" character varying, "parentCategoryId" character varying, CONSTRAINT "UQ_8b0be371d28245da6e4f4b61878" UNIQUE ("name"), CONSTRAINT "PK_24dbc6126a28ff948da33e97d3b" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."products_status_enum" AS ENUM('Available', 'OutOfStock', 'Discontinued', 'Pending')`,
    );
    await queryRunner.query(
      `CREATE TABLE "products" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "sku" character varying(100) NOT NULL, "status" "public"."products_status_enum" NOT NULL DEFAULT 'Available', "price" jsonb NOT NULL, "name" character varying(255) NOT NULL, "description" text NOT NULL, "discontinuedAt" TIMESTAMP, "stock" integer NOT NULL DEFAULT '0', "category_id" character varying, CONSTRAINT "UQ_c44ac33a05b144dd0d9ddcf9327" UNIQUE ("sku"), CONSTRAINT "PK_0806c755e0aca124e67c0cf6d7d" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TABLE "order_items" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "productId" character varying NOT NULL, "quantity" integer NOT NULL, "priceAtPurchase" jsonb NOT NULL, "orderId" character varying NOT NULL, CONSTRAINT "PK_005269d8574e6fac0493715c308" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."shipments_shipmentstatus_enum" AS ENUM('Pending', 'Shipped', 'InTransit', 'Delivered', 'Returned', 'Cancelled')`,
    );
    await queryRunner.query(
      `CREATE TABLE "shipments" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "shipmentStatus" "public"."shipments_shipmentstatus_enum" NOT NULL DEFAULT 'Pending', "trackingNumber" character varying(255) NOT NULL, "shippedAt" TIMESTAMP, "estimatedArrival" TIMESTAMP, "order_id" character varying, CONSTRAINT "REL_e86fac2a18a75dcb82bfbb23f4" UNIQUE ("order_id"), CONSTRAINT "PK_6deda4532ac542a93eab214b564" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."payments_paymentstatus_enum" AS ENUM('Pending', 'Completed', 'Paid', 'Failed', 'Refunded', 'Cancelled')`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."payments_paymentmethod_enum" AS ENUM('creditCard', 'Paypal', 'BankTransfer', 'CashOnDelivery', 'ApplePay', 'GooglePay', 'Cryptocurrency')`,
    );
    await queryRunner.query(
      `CREATE TABLE "payments" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "paymentStatus" "public"."payments_paymentstatus_enum" NOT NULL DEFAULT 'Pending', "paymentMethod" "public"."payments_paymentmethod_enum" NOT NULL, "amount" jsonb NOT NULL, "paymentDate" TIMESTAMP NOT NULL DEFAULT now(), "order_id" character varying, "user_id" character varying, CONSTRAINT "REL_b2f7b823a21562eeca20e72b00" UNIQUE ("order_id"), CONSTRAINT "PK_197ab7af18c93fbb0c9b28b4a59" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."orders_status_enum" AS ENUM('Pending', 'Shipped', 'Cancelled')`,
    );
    await queryRunner.query(
      `CREATE TABLE "orders" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "totalPrice" jsonb NOT NULL, "status" "public"."orders_status_enum" NOT NULL DEFAULT 'Shipped', "user_id" character varying, "shipment_id" character varying, CONSTRAINT "REL_b17a0214fe7415e3cdc38923a4" UNIQUE ("shipment_id"), CONSTRAINT "PK_710e2d4957aa5878dfe94e4ac2f" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TABLE "users" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "email" character varying NOT NULL, "password" character varying NOT NULL, CONSTRAINT "PK_a3ffb1c0c8416b9fc6f907b7433" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TABLE "cart_items" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "cartId" character varying NOT NULL, "productId" character varying NOT NULL, "quantity" integer NOT NULL, "price" jsonb NOT NULL, CONSTRAINT "PK_6fccf5ec03c172d27a28a82928b" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `CREATE TABLE "carts" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "userId" character varying NOT NULL, "totalPrice" jsonb NOT NULL, CONSTRAINT "PK_b5f695a59f5ebb50af3c8160816" PRIMARY KEY ("id"))`,
    );
    await queryRunner.query(
      `ALTER TABLE "categories" ADD CONSTRAINT "FK_ccde635bce518afe7c110858cc4" FOREIGN KEY ("parentCategoryId") REFERENCES "categories"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "products" ADD CONSTRAINT "FK_9a5f6868c96e0069e699f33e124" FOREIGN KEY ("category_id") REFERENCES "categories"("id") ON DELETE SET NULL ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "order_items" ADD CONSTRAINT "FK_f1d359a55923bb45b057fbdab0d" FOREIGN KEY ("orderId") REFERENCES "orders"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "shipments" ADD CONSTRAINT "FK_e86fac2a18a75dcb82bfbb23f43" FOREIGN KEY ("order_id") REFERENCES "orders"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" ADD CONSTRAINT "FK_b2f7b823a21562eeca20e72b006" FOREIGN KEY ("order_id") REFERENCES "orders"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" ADD CONSTRAINT "FK_427785468fb7d2733f59e7d7d39" FOREIGN KEY ("user_id") REFERENCES "users"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "orders" ADD CONSTRAINT "FK_a922b820eeef29ac1c6800e826a" FOREIGN KEY ("user_id") REFERENCES "users"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "orders" ADD CONSTRAINT "FK_b17a0214fe7415e3cdc38923a49" FOREIGN KEY ("shipment_id") REFERENCES "shipments"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD CONSTRAINT "FK_edd714311619a5ad09525045838" FOREIGN KEY ("cartId") REFERENCES "carts"("id") ON DELETE CASCADE ON UPDATE NO ACTION`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "cart_items" DROP CONSTRAINT "FK_edd714311619a5ad09525045838"`,
    );
    await queryRunner.query(
      `ALTER TABLE "orders" DROP CONSTRAINT "FK_b17a0214fe7415e3cdc38923a49"`,
    );
    await queryRunner.query(
      `ALTER TABLE "orders" DROP CONSTRAINT "FK_a922b820eeef29ac1c6800e826a"`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" DROP CONSTRAINT "FK_427785468fb7d2733f59e7d7d39"`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" DROP CONSTRAINT "FK_b2f7b823a21562eeca20e72b006"`,
    );
    await queryRunner.query(
      `ALTER TABLE "shipments" DROP CONSTRAINT "FK_e86fac2a18a75dcb82bfbb23f43"`,
    );
    await queryRunner.query(
      `ALTER TABLE "order_items" DROP CONSTRAINT "FK_f1d359a55923bb45b057fbdab0d"`,
    );
    await queryRunner.query(
      `ALTER TABLE "products" DROP CONSTRAINT "FK_9a5f6868c96e0069e699f33e124"`,
    );
    await queryRunner.query(
      `ALTER TABLE "categories" DROP CONSTRAINT "FK_ccde635bce518afe7c110858cc4"`,
    );
    await queryRunner.query(`DROP TABLE "carts"`);
    await queryRunner.query(`DROP TABLE "cart_items"`);
    await queryRunner.query(`DROP TABLE "users"`);
    await queryRunner.query(`DROP TABLE "orders"`);
    await queryRunner.query(`DROP TYPE "public"."orders_status_enum"`);
    await queryRunner.query(`DROP TABLE "payments"`);
    await queryRunner.query(`DROP TYPE "public"."payments_paymentmethod_enum"`);
    await queryRunner.query(`DROP TYPE "public"."payments_paymentstatus_enum"`);
    await queryRunner.query(`DROP TABLE "shipments"`);
    await queryRunner.query(
      `DROP TYPE "public"."shipments_shipmentstatus_enum"`,
    );
    await queryRunner.query(`DROP TABLE "order_items"`);
    await queryRunner.query(`DROP TABLE "products"`);
    await queryRunner.query(`DROP TYPE "public"."products_status_enum"`);
    await queryRunner.query(`DROP TABLE "categories"`);
  }
}
