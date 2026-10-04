# Bash completion for amcli, the AngouriMath terminal: the command after it.
_amcli()
{
    local current="${COMP_WORDS[COMP_CWORD]}"
    if [ "$COMP_CWORD" -eq 1 ]; then
        COMPREPLY=( $(compgen -W "eval simp fsimp diff solve sub latex info mcp help" -- "$current") )
    elif [ "$COMP_CWORD" -eq 2 ] && [ "${COMP_WORDS[1]}" = mcp ]; then
        COMPREPLY=( $(compgen -W "--selftest --help" -- "$current") )
    fi
}
complete -F _amcli amcli angourimath-terminal
