{

const COMMA = {
    scope: 'symbol',
    begin: ','
};

const PERIOD = {
    scope: 'symbol',
    begin: '\\.'
};


hljs.registerLanguage('imaginarium', function () {
    return {
        keywords: {
            symbol: ['and', 'or', 'not',
                'can be', 'can', 'cannot', 'must', 'should',
                'are', 'is', 'be', 'implies',
                'exist',
                'kind', 'way',
                'of', 'from', 'at least', 'at most', 'between',
                'a', 'A', 'An', 'an',
                '.',
                'has', 'have',
                'mutually', 'exclusive',
                'some', 'many',
                'each', 'other',
                'all', 'every', 'any', 'exactly',
                'rare', 'common',
                'always', 'itself',
                'plural', 'singular',
                'identified', 'described',
                'mention', 'print',
                'called', 'its', 'themselves', 'themself',
                'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten',
            ]
        },
        contains: [ COMMA, PERIOD, ]
    }
})
}