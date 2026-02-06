"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.MongoService = void 0;
const mongodb_1 = require("mongodb");
const config_1 = require("../config");
class MongoService {
    constructor(_mongo) {
        this._mongo = _mongo;
    }
    static async init() {
        try {
            const _mongo = await mongodb_1.MongoClient.connect(config_1.Config.mongo.connectionString);
            return new MongoService(_mongo);
        }
        catch (err) {
            console.error(`Failed connecting to mongo: ${err}`);
            throw err;
        }
    }
    async insert(collectionName, document) {
        const db = this._mongo.db(config_1.Config.mongo.connection.db);
        const collection = db.collection(collectionName);
        await collection.insertOne(document);
    }
}
exports.MongoService = MongoService;
//# sourceMappingURL=mongo.js.map