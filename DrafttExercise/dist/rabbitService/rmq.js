"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.RabbitmqService = void 0;
const amqplib = require("amqplib");
const config_1 = require("../config");
class RabbitmqService {
    constructor(_rmq, _channel) {
        this._rmq = _rmq;
        this._channel = _channel;
    }
    static async init() {
        try {
            const _rmq = await amqplib.connect(config_1.Config.rabbitMq.connection);
            const _channel = (await _rmq.createConfirmChannel());
            await _channel.assertQueue(config_1.Config.rabbitMq.queueConfig.queue);
            await _channel.assertExchange(config_1.Config.rabbitMq.queueConfig.exchange, 'topic');
            await _channel.bindQueue(config_1.Config.rabbitMq.queueConfig.queue, config_1.Config.rabbitMq.queueConfig.exchange, '#');
            _channel.prefetch(config_1.Config.rabbitMq.prefetchCount);
            return new RabbitmqService(_rmq, _channel);
        }
        catch (error) {
            console.error(`Failed connecting to RMQ: ${error}`);
            throw error;
        }
    }
    async publish(message, options) {
        const routingKey = (options === null || options === void 0 ? void 0 : options.routingKey) || '#';
        const exchange = (options === null || options === void 0 ? void 0 : options.exchange) || config_1.Config.rabbitMq.queueConfig.exchange;
        await new Promise((res, rej) => {
            this._channel.publish(exchange, routingKey, Buffer.from(JSON.stringify(message)), { contentType: 'application/json' }, (err, _) => {
                if (err) {
                    rej(err);
                }
                else {
                    res('Message published');
                }
            });
        });
    }
    async subscribe(queueName, callback) {
        const queue = queueName || config_1.Config.rabbitMq.queueConfig.queue;
        await this._channel.consume(queueName, async (message) => {
            if (!message) {
                throw new Error('Recieved null message');
            }
            await callback(message);
            this._channel.ack(message);
        });
    }
}
exports.RabbitmqService = RabbitmqService;
//# sourceMappingURL=rmq.js.map