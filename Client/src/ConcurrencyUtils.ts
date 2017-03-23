
/// <reference path="../../Server/types-gtanetwork/index.d.ts" />

export interface requestResponseFlowOptions<TData> {
    serverEventName : string;
    clientEventName? : string;
    millisecondsTimeout? : number;
    data? : TData
}



export async function requestResponseFlow<TResult,TData>(api : GTANetwork.Javascript.ScriptContext, opts : requestResponseFlowOptions<TData>) : Promise<TResult> {
    return new Promise<TResult>(resolve => {
        const handler = (eventName : string, args: System.Array<any>) => {
            if (eventName === opts.clientEventName) {
                const data = JSON.parse(args[0]) as TResult;
                resolve(data);
            }
        }
        if (opts.clientEventName)
            api.onServerEventTrigger.connect(handler);
        api.triggerServerEvent(opts.serverEventName,JSON.stringify(opts.data || null));
    });
}