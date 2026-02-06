#! /usr/bin/env node

import { StreetsService } from "./israelistreets/StreetsService"
import { PostgresService } from "./postgresService/postgres"
import { RabbitmqService } from "./rabbitService/rmq"
import { city, cities } from "./israelistreets/cities"

const isValidCity = (cityName: string): cityName is city => {
    return cityName in cities
}

const processCity = async (cityName: city, rabbitmq: RabbitmqService) => {
    console.log(`[Publisher] Processing city: ${cityName}`)
    const streetInfo = await StreetsService.getStreetsInCity(cityName)
    console.log(`[Publisher] Found ${streetInfo.streets.length} streets in ${cityName}`)
    
    await Promise.all(streetInfo.streets.map(async (street) => {
        console.log(`[Publisher] Publishing street: ${street.street_name} in city: ${cityName}`)
        await rabbitmq.publish({streetId: street.streetId})
    }))
    
    console.log(`[Publisher] Finished publishing streets for ${cityName}`)
}

const main = async (cityList: string[]) => {
    if (cityList.length === 0) {
        console.error('Usage: cityStreets <city1> [city2] [city3] ...')
        console.error('Example: cityStreets Itamar Ashdod "Tel Aviv"')
        process.exit(1)
    }

    console.log(`[Publisher] Starting to process ${cityList.length} cities: ${cityList.join(', ')}`)
    
    await PostgresService.init()
    const rabbitmq = await RabbitmqService.init()
    
    for (const cityName of cityList) {
        if (!isValidCity(cityName)) {
            console.error(`[Publisher] Error: "${cityName}" is not a valid city name. Skipping...`)
            continue
        }
        await processCity(cityName, rabbitmq)
    }
    
    console.log(`[Publisher] Done! Processed all ${cityList.length} cities.`)
    process.exit(0)
}

const requestedCities = process.argv.slice(2)
main(requestedCities)