:- include('findactions').
:- initialization(main).

main :-
    opt_arguments([], _, [H|T]),
    append(Beliefs, Intentions, H, T),
    getRecommendedActions(standard,Beliefs,Intentions,Actions),
    write(Actions),
    halt(0).

append([], [], break, []) :- !.

append([], List2, break, [H|T]) :-
    appendRest(List2, H, T).

append([Belief|List1], List2, Belief, [H|T]) :-
    append(List1, List2, H, T).

appendRest([H|[]], H, []) :- !.

appendRest([Intention|List2], Intention, [H|T]) :-
    appendRest(List2, H, T).