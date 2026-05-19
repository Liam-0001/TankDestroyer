import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import {HubConnectionBuilder, LogLevel} from "@microsoft/signalr";
import { values } from "../Values";
import { GameResult } from "../Objects/GameResult";
export function useBattleResults() {
    const queryClient = useQueryClient();

    const query = useQuery<GameResult[]>({
        queryKey: ["battleResults"],
        queryFn: () => Promise.resolve([]),
    });

    useEffect(() => {
        let stopped = false;

        const hub = new HubConnectionBuilder()
            .withUrl(`${values.Address}/battlehub`)
            .withAutomaticReconnect()
            .build();

        hub.start()
            .catch(() => {})
            .then(() => {
            if (stopped) { hub.stop(); return; }
            hub.on("ReceiveResult", (result: GameResult) => {
                console.log("ReceiveResult", result);
                queryClient.setQueryData<GameResult[]>(["battleResults"], (old) => {
                    const existing = old?.findIndex(
                        (r) => r.botName === result.botName && r.mapName === result.mapName && r.creator === result.creator
                    );
                    if (existing !== undefined && existing >= 0) {
                        const updated = [...(old ?? [])];
                        updated[existing] = result;
                        return updated;
                    }
                    return [...(old ?? []), result];
                });
            });
        });

        return () => { stopped = true; hub.stop(); };
    }, [queryClient]);

    return query;
}