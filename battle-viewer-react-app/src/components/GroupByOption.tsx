import {FormControl, InputLabel, MenuItem, Select} from "@mui/material";
import {ChangeEvent} from "react";
import {values} from "../Values";

interface GroupByProps{
    selected: string,
    onChange: (value:string) => void
}

export default function GroupByOption({selected,onChange}: GroupByProps) {
    return (
        <FormControl>
            <InputLabel>Group by</InputLabel>
            <Select value={selected} label="Group by" onChange={(e)=> onChange(e.target.value)}>
                <MenuItem value="creator">Name</MenuItem>
                <MenuItem value="botName">Bot Name</MenuItem>
                <MenuItem value="mapName">Map</MenuItem>
            </Select>
        </FormControl>
    );
}