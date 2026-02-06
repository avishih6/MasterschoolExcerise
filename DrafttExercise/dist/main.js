#! /usr/bin/env node
"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const StreetsService_1 = require("./israelistreets/StreetsService");
const postgres_1 = require("./postgresService/postgres");
const rmq_1 = require("./rabbitService/rmq");
const cities_1 = require("./israelistreets/cities");
const isValidCity = (cityName) => {
    return cityName in cities_1.cities;
};
const processCity = async (cityName, rabbitmq) => {
    console.log(`[Publisher] Processing city: ${cityName}`);
    const streetInfo = await StreetsService_1.StreetsService.getStreetsInCity(cityName);
    console.log(`[Publisher] Found ${streetInfo.streets.length} streets in ${cityName}`);
    await Promise.all(streetInfo.streets.map(async (street) => {
        console.log(`[Publisher] Publishing street: ${street.street_name} in city: ${cityName}`);
        await rabbitmq.publish({ streetId: street.streetId });
    }));
    console.log(`[Publisher] Finished publishing streets for ${cityName}`);
};
const main = async (cityList) => {
    if (cityList.length === 0) {
        console.error('Usage: cityStreets <city1> [city2] [city3] ...');
        console.error('Example: cityStreets Itamar Ashdod "Tel Aviv"');
        process.exit(1);
    }
    console.log(`[Publisher] Starting to process ${cityList.length} cities: ${cityList.join(', ')}`);
    await postgres_1.PostgresService.init();
    const rabbitmq = await rmq_1.RabbitmqService.init();
    for (const cityName of cityList) {
        if (!isValidCity(cityName)) {
            console.error(`[Publisher] Error: "${cityName}" is not a valid city name. Skipping...`);
            continue;
        }
        await processCity(cityName, rabbitmq);
    }
    console.log(`[Publisher] Done! Processed all ${cityList.length} cities.`);
    process.exit(0);
};
const requestedCities = process.argv.slice(2);
main(requestedCities);
//# sourceMappingURL=main.js.map