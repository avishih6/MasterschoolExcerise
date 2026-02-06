import { Config } from "./config"
import { StreetsService } from "./israelistreets/StreetsService"
import { PostgresService } from "./postgresService/postgres"
import { RabbitmqService } from "./rabbitService/rmq"

export async function consume() {
    const rabbitmq = await RabbitmqService.init()
    const pgService = await PostgresService.init()
    await rabbitmq.subscribe(Config.rabbitMq.queueConfig.queue, async (message) => {
        const streetData = JSON.parse(message.content.toString())
        const street = await StreetsService.getStreetInfoById(streetData.streetId)
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
        ]
        console.log(`[Consumer] Upserting street: ${street.street_name} in city: ${street.city_name}`)
        await pgService.query(`
            INSERT INTO ${Config.postgres.dbConfig.streetsTableName}
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
        `, streetValues)
    })
}
consume()