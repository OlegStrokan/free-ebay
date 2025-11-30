import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1735850190404 implements MigrationInterface {
  name = 'Migrations1735850190404';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "cart_items" DROP CONSTRAINT "FK_72679d98b31c737937b8932ebe6"`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "createdAt" TIMESTAMP NOT NULL DEFAULT now()`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD "updatedAt" TIMESTAMP NOT NULL DEFAULT now()`,
    );
    await queryRunner.query(
      `ALTER TABLE "cart_items" ALTER COLUMN "productId" SET NOT NULL`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "cart_items" ALTER COLUMN "productId" DROP NOT NULL`,
    );
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "updatedAt"`);
    await queryRunner.query(`ALTER TABLE "cart_items" DROP COLUMN "createdAt"`);
    await queryRunner.query(
      `ALTER TABLE "cart_items" ADD CONSTRAINT "FK_72679d98b31c737937b8932ebe6" FOREIGN KEY ("productId") REFERENCES "products"("id") ON DELETE NO ACTION ON UPDATE NO ACTION`,
    );
  }
}
