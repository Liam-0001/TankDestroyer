import React from 'react';
import logo from './logo.svg';
import './App.css';
import {GameResult} from "./Objects/GameResult";
import {Stack} from "@mui/material";
import DataModule from "./components/DataModule";
import SettingInput from "./components/SettingInput";
import BattleOrganizer from "./components/BattleOrganizer";

function App() {
  const dummyResults: GameResult[] = [
    { botName: "IronTank", creator: "Alice", mapName: "Desert Storm", wins: 12, losses: 3, stalemates: 1, crashes: 0 },
    { botName: "BronzeTank", creator: "Alice", mapName: "Arctic Tundra", wins: 7, losses: 5, stalemates: 2, crashes: 0 },
    { botName: "IronTank", creator: "Alice", mapName: "Urban Jungle", wins: 9, losses: 4, stalemates: 1, crashes: 1 },
    { botName: "SteelBehemoth", creator: "Bob", mapName: "Arctic Tundra", wins: 8, losses: 7, stalemates: 2, crashes: 1 },
    { botName: "SteelBehemoth", creator: "Bob", mapName: "Desert Storm", wins: 5, losses: 9, stalemates: 1, crashes: 0 },
    { botName: "ThunderTrack", creator: "Charlie", mapName: "Urban Jungle", wins: 15, losses: 1, stalemates: 0, crashes: 0 },
    { botName: "ThunderTrack", creator: "Charlie", mapName: "Volcanic Ridge", wins: 11, losses: 3, stalemates: 1, crashes: 0 },
    { botName: "RustyBarrel", creator: "Diana", mapName: "Desert Storm", wins: 4, losses: 10, stalemates: 3, crashes: 2 },
    { botName: "RustyBarrel", creator: "Diana", mapName: "Arctic Tundra", wins: 6, losses: 8, stalemates: 2, crashes: 1 },
    { botName: "NightCrawler", creator: "Eve", mapName: "Volcanic Ridge", wins: 9, losses: 5, stalemates: 1, crashes: 0 },
    { botName: "NightCrawler", creator: "Eve", mapName: "Urban Jungle", wins: 7, losses: 6, stalemates: 2, crashes: 0 },
    { botName: "PhantomShell", creator: "Frank", mapName: "Arctic Tundra", wins: 11, losses: 4, stalemates: 2, crashes: 1 },
    { botName: "PhantomShell", creator: "Frank", mapName: "Desert Storm", wins: 8, losses: 6, stalemates: 1, crashes: 0 },
    { botName: "BlazingTread", creator: "Grace", mapName: "Urban Jungle", wins: 6, losses: 8, stalemates: 0, crashes: 3 },
    { botName: "BlazingTread", creator: "Grace", mapName: "Volcanic Ridge", wins: 10, losses: 4, stalemates: 1, crashes: 1 },
    { botName: "CopperCannon", creator: "Hank", mapName: "Volcanic Ridge", wins: 13, losses: 2, stalemates: 1, crashes: 0 },
    { botName: "CopperCannon", creator: "Hank", mapName: "Urban Jungle", wins: 9, losses: 5, stalemates: 2, crashes: 0 },
    { botName: "VoidRampart", creator: "Isla", mapName: "Desert Storm", wins: 7, losses: 6, stalemates: 4, crashes: 1 },
    { botName: "VoidRampart", creator: "Isla", mapName: "Arctic Tundra", wins: 5, losses: 8, stalemates: 3, crashes: 0 },
    { botName: "TitanPlating", creator: "Jack", mapName: "Arctic Tundra", wins: 10, losses: 5, stalemates: 2, crashes: 0 },
    { botName: "TitanPlating", creator: "Jack", mapName: "Desert Storm", wins: 8, losses: 6, stalemates: 1, crashes: 1 },
  ];


  return (
      <div className="App">
        <Stack direction={"column"}>
          <SettingInput numberUntilStalemate={100} mapName={""} submit={()=> {}} />
          <BattleOrganizer results={dummyResults}></BattleOrganizer>
        </Stack>
      </div>
  );
}

export default App;
