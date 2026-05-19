import { useMutation } from "@tanstack/react-query";
import { BattleRequest } from "../Objects/BattleRequest";
import { values } from "../Values";

export function useBattle() {
    const { mutateAsync: battle, isPending, isError } = useMutation({
        mutationFn: async (request: BattleRequest) => {
            await fetch(`/api/battle`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(request),
            });
        },
    });

    return { battle, isPending, isError };
}