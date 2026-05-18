import {BattleRequest} from "../Objects/BattleRequest";

import { values } from "../Values";

export function useBattle() {
    const battle = async (request: BattleRequest) => {
        await fetch(`${values.Address}/api/battle`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(request),
        });
    };

    return { battle };
}