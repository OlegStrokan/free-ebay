import { MigrationInterface, QueryRunner } from "typeorm";

export class Migrations1736084625497 implements MigrationInterface {
    name = 'Migrations1736084625497'

    public async up(queryRunner: QueryRunner): Promise<void> {
        await queryRunner.query(`ALTER TABLE "order_items" DROP CONSTRAINT "PK_005269d8574e6fac0493715c308"`);
        await queryRunner.query(`ALTER TABLE "order_items" DROP COLUMN "id"`);
        await queryRunner.query(`ALTER TABLE "order_items" ADD "id" character varying NOT NULL`);
        await queryRunner.query(`ALTER TABLE "order_items" ADD CONSTRAINT "PK_005269d8574e6fac0493715c308" PRIMARY KEY ("id")`);
    }

    public async down(queryRunner: QueryRunner): Promise<void> {
        await queryRunner.query(`ALTER TABLE "order_items" DROP CONSTRAINT "PK_005269d8574e6fac0493715c308"`);
        await queryRunner.query(`ALTER TABLE "order_items" DROP COLUMN "id"`);
        await queryRunner.query(`ALTER TABLE "order_items" ADD "id" uuid NOT NULL DEFAULT uuid_generate_v4()`);
        await queryRunner.query(`ALTER TABLE "order_items" ADD CONSTRAINT "PK_005269d8574e6fac0493715c308" PRIMARY KEY ("id")`);
    }

}
