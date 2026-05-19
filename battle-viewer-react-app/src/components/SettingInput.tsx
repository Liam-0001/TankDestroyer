import {Box, Button, TextField} from "@mui/material";
import { useState } from "react";
import {BattleRequest} from "../Objects/BattleRequest";

interface SettingInputProps {
    numberUntilStalemate: number;
    mapName: string;
    submit: (request: BattleRequest) => void;
}

export default function SettingInput({ numberUntilStalemate, mapName, submit }: SettingInputProps) {
    const [mapNameValue, setMapNameValue] = useState(mapName);
    const [turnsValue, setTurnsValue] = useState(numberUntilStalemate);

    return (
        <Box sx={{ padding: '10px', display: 'flex', flexDirection: 'row', gap: 2 }}>
            <TextField
                label="Map Name"
                type="text"
                value={mapNameValue}
                onChange={(e) => setMapNameValue(e.target.value)}
            />
            <TextField
                label="Turns until stalemate"
                type="number"
                value={turnsValue}
                onChange={(e) => setTurnsValue(Number(e.target.value))}
            />
            <Button onClick={() => {
                console.log("submit clicked", { maxTurns: turnsValue, mapName: mapNameValue });
                submit({ maxTurns: turnsValue, mapName: mapNameValue });
            }}>Submit</Button>
        </Box>
    );
}