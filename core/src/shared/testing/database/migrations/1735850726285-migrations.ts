import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1735850726285 implements MigrationInterface {
  name = 'Migrations1735850726285';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "productId"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "productId" uuid NOT NULL`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "productId"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "productId" character varying NOT NULL`,
    );
  }
}
