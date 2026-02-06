"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PostgresService = void 0;
const pg_1 = require("pg");
const lodash_1 = require("lodash");
const config_1 = require("../config");
class PostgresService {
    static async _initializeDB() {
        var _a;
        const admin = await new pg_1.Pool((0, lodash_1.omit)(config_1.Config.postgres.connection, 'database')).connect();
        try {
            await admin.query(`Create database ${config_1.Config.postgres.dbConfig.dbName}`);
        }
        catch (error) {
            if ((error === null || error === void 0 ? void 0 : error.code) === '42P04' && ((_a = error === null || error === void 0 ? void 0 : error.message) === null || _a === void 0 ? void 0 : _a.includes('already exists'))) {
                console.log(`Database ${config_1.Config.postgres.dbConfig.dbName} already exists, continuing`);
            }
            else {
                console.error(error);
                throw error;
            }
        }
        const pool = new pg_1.Pool(config_1.Config.postgres.connection);
        await pool.connect();
        await pool.query(`Create table if not exists ${config_1.Config.postgres.dbConfig.streetsTableName}(
      street_id INT PRIMARY KEY,
      region_code INT,
      region_name TEXT,
      city_code INT,
      city_name TEXT,
      street_code INT,
      street_name TEXT,
      street_name_status TEXT,
      official_code INT,
      created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
      updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
    )`);
        return pool;
    }
    static async init() {
        if (!this._pool) {
            this._pool = await this._initializeDB();
            return new PostgresService(this._pool);
        }
        else {
            return new PostgresService(this._pool);
        }
    }
    constructor(pool) {
        this.pool = pool;
    }
    async query(text, params) {
        try {
            return await this.pool.query(text, params);
        }
        catch (error) {
            console.error(`Error executing query: ${text}, ${params}, error: ${error}`);
            throw error;
        }
    }
}
exports.PostgresService = PostgresService;
//# sourceMappingURL=postgres.js.map