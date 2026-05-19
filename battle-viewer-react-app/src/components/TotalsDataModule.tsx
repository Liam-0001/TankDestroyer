import {ReactNode} from "react";
import {
    Box, Divider, Paper, styled, Table, TableBody, TableCell,
    tableCellClasses, TableContainer, TableHead, TableRow, Typography
} from "@mui/material";
import {GameResult} from "../Objects/GameResult";

interface SortModuleInterface {
    name: string;
    results: GameResult[]
}


const StyledTableRow = styled(TableRow)(({ theme }) => ({
    '&:nth-of-type(odd)': {
        backgroundColor: theme.palette.action.hover,
    },
    // hide last border
    '&:last-child td, &:last-child th': {
        border: 0,
    },
}));

const StyledTableCell = styled(TableCell)(({ theme }) => ({
    [`&.${tableCellClasses.head}`]: {
        backgroundColor: theme.palette.common.black,
        color: theme.palette.common.white,
    },
    [`&.${tableCellClasses.body}`]: {
        fontSize: 14,
    },
}));


export default function TotalsDataModule({name, results}: SortModuleInterface) {
    return (
        <Box sx={{ padding: '10px', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
        <Typography variant="h6" component="div">
                {name}
            </Typography>
            <Divider sx={{ width: '100%' }} />
<br/>
            <TableContainer component={Paper} sx={{ maxWidth: 800 }}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <StyledTableCell>Player</StyledTableCell>
                            <StyledTableCell>Botname</StyledTableCell>
                            <StyledTableCell align="right">Wins</StyledTableCell>
                            <StyledTableCell align="right">Losses</StyledTableCell>
                            <StyledTableCell align="right">Stalemates</StyledTableCell>
                            <StyledTableCell align="right">Crashes</StyledTableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {results.map((row) => (
                            <StyledTableRow key={`${row.creator}-${row.botName}`}>
                                <StyledTableCell component="th" scope="row">
                                    {row.creator}
                                </StyledTableCell>
                                <StyledTableCell>{row.botName}</StyledTableCell>
                                <StyledTableCell align="right">{row.wins}</StyledTableCell>
                                <StyledTableCell align="right">{row.losses}</StyledTableCell>
                                <StyledTableCell align="right">{row.stalemates}</StyledTableCell>
                                <StyledTableCell align="right">{row.crashes}</StyledTableCell>
                            </StyledTableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Box>
    )
}