
import { MongoClient } from "mongodb"
import { Config } from "../config"


export class MongoService {
    constructor(
        private _mongo: MongoClient
    ) {}
    static async init() {
        try {
            const  _mongo = await MongoClient.connect(Config.mongo.connectionString)
            return new MongoService(_mongo)
        }
        catch (err) {
            console.error(`Failed connecting to mongo: ${err}`)
            throw err
        }
    }

    async insert<T>(collectionName: string, document: T) {
        const db = this._mongo.db(Config.mongo.connection.db)
        const collection = db.collection(collectionName)
        await collection.insertOne(document)
    }
}
