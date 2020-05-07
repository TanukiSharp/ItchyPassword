type ServiceContainer = { [key: string]: any };

const services: ServiceContainer = {};

export function listServices(): string[] {
    return Object.keys(services);
}

export function getService(serviceName: string): any {
    if (!serviceName) {
        throw new TypeError(`Argument 'serviceName' is mandatory.`);
    }

    const service = services[serviceName];

    if (service === undefined) {
        throw new Error(`Service '${serviceName}' is not registered.`);
    }

    return service;
}

export function registerService(serviceName: string, instance: any): void {
    if (!serviceName) {
        throw new TypeError(`Argument 'serviceName' is mandatory.`);
    }
    if (instance === undefined) {
        throw new TypeError(`Argument 'instance' cannot be undefined.`);
    }

    if (services[serviceName] !== undefined) {
        throw new Error(`Service '${serviceName}' is already registered.`);
    }

    services[serviceName] = instance;
}
