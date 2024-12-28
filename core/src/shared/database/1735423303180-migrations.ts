import { MigrationInterface, QueryRunner } from "typeorm";

export class Migrations1735423303180 implements MigrationInterface {
    name = 'Migrations1735423303180'

    public async up(queryRunner: QueryRunner): Promise<void> {
        await queryRunner.query(`CREATE TABLE "user_db" ("createdAt" TIMESTAMP NOT NULL DEFAULT now(), "updatedAt" TIMESTAMP NOT NULL DEFAULT now(), "id" character varying NOT NULL, "email" character varying NOT NULL, "password" character varying NOT NULL, CONSTRAINT "PK_3a30f4ab478851bfcd2d028105a" PRIMARY KEY ("id"))`);
        await queryRunner.query(`CREATE TABLE "token_db" ("id" character varying NOT NULL, "userId" character varying NOT NULL, "accessToken" character varying NOT NULL, "refreshToken" character varying NOT NULL, CONSTRAINT "PK_c64da7146a2f1ff6193a2a195a2" PRIMARY KEY ("id"))`);
    }

    public async down(queryRunner: QueryRunner): Promise<void> {
        await queryRunner.query(`DROP TABLE "token_db"`);
        await queryRunner.query(`DROP TABLE "user_db"`);
    }

}
