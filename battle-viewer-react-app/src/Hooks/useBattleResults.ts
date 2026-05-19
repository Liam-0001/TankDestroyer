import { useEffect, useRef, useState } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { values } from "../Values";
import { GameResult } from "../Objects/GameResult";

export function useBattleResults() {
    const [results, setResults] = useState<GameResult[]>([]);

    useEffect(() => {
        let stopped = false;

        const hub = new HubConnectionBuilder()
            .withUrl(`/battlehub`)
            .withAutomaticReconnect()
            .build();

        hub.start()
            .catch(() => {})
            .then(() => {
                if (stopped) { hub.stop(); return; }
                hub.on("ReceiveResult", (result: GameResult) => {
                    console.log("ReceiveResult", result);
                    setResults(old => {
                        const existing = old.findIndex(
                            (r) => r.botName === result.botName && r.mapName === result.mapName && r.creator === result.creator
                        );
                        if (existing >= 0) {
                            const updated = [...old];
                            updated[existing] = result;
                            return updated;
                        }
                        return [...old, result];
                    });
                });
            });

        return () => { stopped = true; hub.stop(); };
    }, []);

    return results;
}