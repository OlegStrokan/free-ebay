import { MigrationInterface, QueryRunner } from 'typeorm';

export class UpdatePaymentMethod1751400602828 implements MigrationInterface {
  name = 'UpdatePaymentMethod1751400602828';

  public async up(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "payments" ADD "paymentIntentId" character varying(100)`,
    );
    await queryRunner.query(
      `ALTER TYPE "public"."payments_paymentmethod_enum" RENAME TO "payments_paymentmethod_enum_old"`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."payments_paymentmethod_enum" AS ENUM('Card', 'Paypal', 'BankTransfer', 'CashOnDelivery', 'ApplePay', 'GooglePay', 'Cryptocurrency')`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" ALTER COLUMN "paymentMethod" TYPE "public"."payments_paymentmethod_enum" USING "paymentMethod"::"text"::"public"."payments_paymentmethod_enum"`,
    );
    await queryRunner.query(
      `DROP TYPE "public"."payments_paymentmethod_enum_old"`,
    );
    await queryRunner.query(
      `ALTER TABLE "shipments" DROP COLUMN "trackingNumber"`,
    );
    await queryRunner.query(
      `ALTER TABLE "shipments" ADD "trackingNumber" character varying(80) NOT NULL`,
    );
  }

  public async down(queryRunner: QueryRunner): Promise<void> {
    await queryRunner.query(
      `ALTER TABLE "shipments" DROP COLUMN "trackingNumber"`,
    );
    await queryRunner.query(
      `ALTER TABLE "shipments" ADD "trackingNumber" character varying(255) NOT NULL`,
    );
    await queryRunner.query(
      `CREATE TYPE "public"."payments_paymentmethod_enum_old" AS ENUM('creditCard', 'Paypal', 'BankTransfer', 'CashOnDelivery', 'ApplePay', 'GooglePay', 'Cryptocurrency')`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" ALTER COLUMN "paymentMethod" TYPE "public"."payments_paymentmethod_enum_old" USING "paymentMethod"::"text"::"public"."payments_paymentmethod_enum_old"`,
    );
    await queryRunner.query(`DROP TYPE "public"."payments_paymentmethod_enum"`);
    await queryRunner.query(
      `ALTER TYPE "public"."payments_paymentmethod_enum_old" RENAME TO "payments_paymentmethod_enum"`,
    );
    await queryRunner.query(
      `ALTER TABLE "payments" DROP COLUMN "paymentIntentId"`,
    );
  }
}
