import { MigrationInterface, QueryRunner } from 'typeorm';

export class Migrations1737479055587 implements MigrationInterface {
  name = 'Migrations1737479055587';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      ` ALTER TABLE "shipments"  ADD "address" character varying(80) NOT NULL    DEFAULT ''`,
    );

    await queryRunner.query(
      `  ALTER TABLE "shipments"   ALTER COLUMN "address" DROP DEFAULT `,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(`ALTER TABLE "shipments" DROP COLUMN "address"`);
  }
}
