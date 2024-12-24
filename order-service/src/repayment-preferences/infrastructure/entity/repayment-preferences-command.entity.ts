import { OrderCommand } from 'src/order/infrastructure/entity/order-command.entity';
import { Entity, PrimaryColumn, Column, ManyToOne } from 'typeorm';

@Entity('repayment_preferences_command')
export class RepaymentPreferencesCommand {
    @PrimaryColumn()
    id: string;

    @Column()
    customerId: string;

    @Column()
    repaymentFrequency: string;

    @Column()
    repaymentType: string;

    @Column({ nullable: true })
    bankAccountPrefix: string;

    @Column({ nullable: true })
    bankAccountNumber: string;

    @Column({ nullable: true })
    bankCode: string;

    @Column({ nullable: true })
    iban: string;

    @Column({ nullable: true })
    bic: string;

    @ManyToOne(() => OrderCommand, (order) => order.repaymentPreferences)
    order: OrderCommand;
}
