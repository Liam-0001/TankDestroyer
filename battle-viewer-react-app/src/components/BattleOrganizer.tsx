import { useState } from "react";
import {Box, FormControl, InputLabel, Select, MenuItem, Grid} from "@mui/material";
import {GameResult} from "../Objects/GameResult";
import DataModule from "./DataModule";
import GroupByOption from "./GroupByOption";
import TotalsDataModule from "./TotalsDataModule";

type GroupBy = "creator" | "botName" | "mapName";

export default function BattleOrganizer({ results }: { results: GameResult[] }) {
    const [groupByValue, setgroupByValue] = useState<GroupBy>("mapName");

    const grouped = results.reduce((acc, result) => {
        const key = result[groupByValue];
        acc[key] ??= [];
        acc[key].push(result);
        return acc;
    }, {} as Record<string, GameResult[]>);



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

    return (
        <Grid container spacing={2}>
            <Grid size={{ xs: 6, md: 10}}>

                <TotalsDataModule name={"Totals"} results={cumulativeByCreatorBot}/>

                {Object.entries(grouped).map(([key, groupResults]) => (
                    <DataModule key={key} name={key} results={groupResults} />
                ))}
            </Grid>

            <Grid size={{ xs: 6, md: 2 }}>
                <GroupByOption selected={ groupByValue} onChange={(value)=>setgroupByValue(value as GroupBy)} />

            </Grid>
        </Grid>

    );
}