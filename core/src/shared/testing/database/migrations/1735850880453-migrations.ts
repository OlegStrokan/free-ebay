import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1735850880453 implements MigrationInterface {
  name = 'Migrations1735850880453';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "cart_items" DROP CONSTRAINT "PK_6fccf5ec03c172d27a28a82928b"`,
    );
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "id"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "id" character varying NOT NULL`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD CONSTRAINT "PK_6fccf5ec03c172d27a28a82928b" PRIMARY KEY ("id")`,
    );
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "productId"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "productId" character varying NOT NULL`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "productId"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "productId" uuid NOT NULL`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" DROP CONSTRAINT "PK_6fccf5ec03c172d27a28a82928b"`,
    );
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "id"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "id" uuid NOT NULL DEFAULT uuid_generate_v4()`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD CONSTRAINT "PK_6fccf5ec03c172d27a28a82928b" PRIMARY KEY ("id")`,
    );
  }
}
