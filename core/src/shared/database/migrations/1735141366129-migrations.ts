import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1735141366129 implements MigrationInterface {
  name = 'Migrations1735141366129';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `CREATE TYPE "public"."products_status_enum" AS ENUM('Available', 'OutOfStock', 'Discontinued', 'Pending')`,
    );
    await queryRunner.query(
      `CREATE TABLE "products" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "sku" character varying(100) NOT NULL, "status" "public"."products_status_enum" NOT NULL DEFAULT 'Available', "price" numeric(10,2), "name" character varying(255) NOT NULL, "description" text NOT NULL, "discontinuedAt" TIMESTAMP, CONSTRAINT "UQ_c44ac33a05b144dd0d9ddcf9327" UNIQUE ("sku"), CONSTRAINT "PK_0806c755e0aca124e67c0cf6d7d" PRIMARY KEY ("id"))`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`DROP TABLE "products"`);
    await queryRunner.query(`DROP TYPE "public"."products_status_enum"`);
  }
}
