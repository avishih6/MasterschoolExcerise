"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.Config = void 0;
exports.Config = {
    postgres: {
        get connection() {
            return {
                host: process.env.POSTGRES_HOST || 'localhost',
                user: process.env.POSTGRES_USER || 'postgres',
                password: process.env.POSTGRES_PASSWORD || 'password',
                port: +(process.env.POSTGRES_HOST || 5432),
                database: this.dbConfig.dbName
            };
        },
        get dbConfig() {
            return {
                dbName: process.env.DB_NAME || 'streets',
                streetsTableName: process.env.STREETS_TABLE_NAME || 'streets'
            };
        }
    },
    rabbitMq: {
        get connection() {
            return {
                hostname: process.env.RABBIT_HOST || 'localhost',
                username: process.env.RABBIT_USER || 'guest',
                password: process.env.RABBIT_PASSWORD || 'guest'
            };
        },
        get queueConfig() {
            return {
                queue: process.env.RABBIT_QUEUE_NAME || 'streets.queue',
                exchange: process.env.RABBIT_EXCHANGE_NAME || 'streets.exchange'
            };
        },
        get prefetchCount() {
            return (+process.env.RABBIT_PREFETCH_COUNT || 10);
        }
    },
    mongo: {
        get connection() {
            return {
                hostname: process.env.MONGO_HOST || 'localhost',
                prefix: process.env.MONGO_PREFIX || 'mongodb',
                db: process.env.MONGO_DATABASE || 'streets',
            };
        },
        get connectionString() {
            const { prefix, hostname, db } = this.connection;
            return `${prefix}://${hostname}/${db}`;
        },
        get collectionName() {
            return process.env.MONGO_STREETS_COLLECTION || 'streets';
        }
    }
};
//# sourceMappingURL=config.js.map