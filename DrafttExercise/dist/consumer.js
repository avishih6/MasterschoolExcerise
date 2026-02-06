"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.consume = void 0;
const config_1 = require("./config");
const StreetsService_1 = require("./israelistreets/StreetsService");
const postgres_1 = require("./postgresService/postgres");
const rmq_1 = require("./rabbitService/rmq");
async function consume() {
    const rabbitmq = await rmq_1.RabbitmqService.init();
    const pgService = await postgres_1.PostgresService.init();
    await rabbitmq.subscribe(config_1.Config.rabbitMq.queueConfig.queue, async (message) => {
        const streetData = JSON.parse(message.content.toString());
        const street = await StreetsService_1.StreetsService.getStreetInfoById(streetData.streetId);
        const streetValues = [
            street.streetId,
            street.region_code,
            street.region_name,
            street.city_code,
            street.city_name,
            street.street_code,
            street.street_name,
            street.street_name_status,
            street.official_code
        ];
        console.log(`[Consumer] Upserting street: ${street.street_name} in city: ${street.city_name}`);
        await pgService.query(`
            INSERT INTO ${config_1.Config.postgres.dbConfig.streetsTableName}
                (street_id, region_code, region_name, city_code, city_name, street_code, street_name, street_name_status, official_code)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
            ON CONFLICT (street_id) DO UPDATE SET
                region_code = EXCLUDED.region_code,
                region_name = EXCLUDED.region_name,
                city_code = EXCLUDED.city_code,
                city_name = EXCLUDED.city_name,
                street_code = EXCLUDED.street_code,
                street_name = EXCLUDED.street_name,
                street_name_status = EXCLUDED.street_name_status,
                official_code = EXCLUDED.official_code,
                updated_at = CURRENT_TIMESTAMP
        `, streetValues);
    });
}
exports.consume = consume;
consume();
//# sourceMappingURL=consumer.js.map