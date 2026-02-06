"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.StreetsService = void 0;
const axios_1 = require("axios");
const lodash_1 = require("lodash");
const cities_1 = require("./cities");
class StreetsService {
    static get axios() {
        if (!this._axios) {
            this._axios = axios_1.default.create({});
        }
        return this._axios;
    }
    static async getStreetsInCity(city) {
        const res = (await this.axios.post('https://data.gov.il/api/3/action/datastore_search', { resource_id: `1b14e41c-85b3-4c21-bdce-9fe48185ffca`, filters: { city_name: cities_1.cities[city] }, limit: 100000 })).data;
        const results = res.result.records;
        if (!results || !results.length) {
            throw new Error('No streets found for city: ' + city);
        }
        const streets = results.map((street) => {
            return { streetId: street._id, street_name: street.street_name.trim() };
        });
        return { city, streets };
    }
    static async getStreetInfoById(id) {
        const res = (await this.axios.post('https://data.gov.il/api/3/action/datastore_search', { resource_id: `1b14e41c-85b3-4c21-bdce-9fe48185ffca`, filters: { _id: id }, limit: 1 })).data;
        const results = res.result.records;
        if (!results || !results.length) {
            throw new Error('No street found for id: ' + id);
        }
        const dbStreet = results[0];
        const cityName = cities_1.englishNameByCity[dbStreet.city_name];
        const street = Object.assign(Object.assign({}, (0, lodash_1.omit)(dbStreet, '_id')), { streetId: dbStreet._id, city_name: cityName, region_name: dbStreet.region_name.trim(), street_name: dbStreet.street_name.trim() });
        return street;
    }
}
exports.StreetsService = StreetsService;
//# sourceMappingURL=StreetsService.js.map