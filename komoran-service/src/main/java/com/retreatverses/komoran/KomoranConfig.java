package com.retreatverses.komoran;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import kr.co.shineware.nlp.komoran.core.Komoran;
import kr.co.shineware.nlp.komoran.model.KomoranResult;
import kr.co.shineware.nlp.komoran.constant.DEFAULT_MODEL;

@Configuration
public class KomoranConfig {
    @Bean
    public Komoran komoran() {
        return new Komoran(DEFAULT_MODEL.FULL);
    }
}
