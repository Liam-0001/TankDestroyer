import React, {useState} from 'react';
import './App.css';
import {Stack} from "@mui/material";
import SettingInput from "./components/SettingInput";
import BattleOrganizer, {GroupBy} from "./components/BattleOrganizer";
import {useBattleResults} from "./Hooks/useBattleResults";
import {useBattle} from "./Hooks/useBattle";
import GroupByOption from "./components/GroupByOption";

function App() {
    const [groupByValue, setgroupByValue] = useState<GroupBy>("mapName");

    const results = useBattleResults()
    const { battle } = useBattle();

    return (
        <div className="App">
            <Stack direction={"column"}>
                <Stack direction={"row"} spacing={4} sx={{ p: 2, alignItems: "center" }}>
                    <SettingInput numberUntilStalemate={100} mapName={""} submit={(request)=> battle(request)} />
                    <GroupByOption selected={groupByValue} onChange={value=> setgroupByValue(value as GroupBy)} />
                </Stack>

                {!results && "No battle has been played"}
                {results && <BattleOrganizer results={results} groupedBy={groupByValue}></BattleOrganizer>}
            </Stack>
        </div>
    );
}

export default App;