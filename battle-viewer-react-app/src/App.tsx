import React from 'react';
import './App.css';
import {Stack} from "@mui/material";
import SettingInput from "./components/SettingInput";
import BattleOrganizer from "./components/BattleOrganizer";
import {useBattleResults} from "./Hooks/useBattleResults";
import {useBattle} from "./Hooks/useBattle";

function App() {

  const { data: results = [] } = useBattleResults()
  const { battle, isPending } = useBattle();

  return (
      <div className="App">
        <Stack direction={"column"}>
          <SettingInput numberUntilStalemate={100} mapName={""} submit={(request)=> battle(request)} />
          {!results && "No battle has been played"}
          {results && <BattleOrganizer results={results}></BattleOrganizer>}
        </Stack>
      </div>
  );
}

export default App;
