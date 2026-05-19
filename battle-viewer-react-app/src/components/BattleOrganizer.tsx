import { Stack} from "@mui/material";
import {GameResult} from "../Objects/GameResult";
import DataModule from "./DataModule";
import TotalsDataModule from "./TotalsDataModule";

export type GroupBy = "creator" | "botName" | "mapName";

export default function BattleOrganizer({ results, groupedBy }: { results: GameResult[], groupedBy: GroupBy }) {
    const cumulativeByCreatorBot = Object.values(
        results.reduce((acc, result) => {
            const key = `${result.creator}-${result.botName}`;
            if (!acc[key]) {
                acc[key] = { creator: result.creator, botName: result.botName, mapName: "", wins: 0, losses: 0, stalemates: 0, crashes: 0 };
            }
            acc[key].wins += result.wins;
            acc[key].losses += result.losses;
            acc[key].stalemates += result.stalemates;
            acc[key].crashes += result.crashes;
            return acc;
        }, {} as Record<string, GameResult>)
    ).sort((a, b) => b.wins - a.wins);

    const grouped = Object.entries(
        results.reduce((acc, result) => {
            const key = result[groupedBy];
            if (!acc[key]) acc[key] = [];
            acc[key].push(result);
            return acc;
        }, {} as Record<string, GameResult[]>)
    ).sort(([a], [b]) => a.localeCompare(b));

    return (
        <Stack spacing={2}  sx={{ width: "100%", px: 2, alignItems:"center" }}>
            <TotalsDataModule name={"Totals"} results={cumulativeByCreatorBot}/>
            {grouped.map(([groupKey, groupResults]) => (
                <DataModule key={groupKey} name={groupKey} results={groupResults}/>
            ))}
        </Stack>
    );
}